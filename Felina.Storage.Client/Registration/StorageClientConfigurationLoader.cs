using System.Text.Json;
using System.Text.RegularExpressions;

namespace Felina.Client;

internal static class StorageClientConfigurationLoader
{
    private static readonly Regex IdPattern = new(
        "^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex CodePattern = new(
        "^[A-Za-z0-9]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    internal static IReadOnlyList<StorageClientRegistrationEntry> Load(string configurationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);

        var directory = Path.GetFullPath(configurationPath.Trim());
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Felina Storage configuration directory '{directory}' does not exist.");

        var files = Directory
            .EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (files.Length == 0)
            throw new InvalidOperationException($"No Felina Storage connection files were found in '{directory}'.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<StorageClientRegistrationEntry>(files.Length);
        foreach (var file in files)
        {
            var source = Deserialize(file);
            var name = source.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                throw Invalid(file, "name is required");
            if (!names.Add(name))
                throw Invalid(file, $"connection name '{name}' is duplicated");

            var id = source.Id?.Trim() ?? string.Empty;
            var code = source.Code?.Trim() ?? string.Empty;
            if (!IdPattern.IsMatch(id))
                throw Invalid(file, "id is required and may contain only letters, numbers, and single hyphens");
            if (!CodePattern.IsMatch(code))
                throw Invalid(file, "code is required and may contain only letters and numbers");
            if (!ids.Add(id))
                throw Invalid(file, $"host id '{id}' is duplicated");
            if (!codes.Add(code))
                throw Invalid(file, $"host code '{code}' is duplicated");
            if (!aliases.Add(name) || !aliases.Add(id) || !aliases.Add(code))
                throw Invalid(file, "name, id, and code must not duplicate another connection alias");
            if (!source.Enabled && source.IsDefault)
                throw Invalid(file, "a disabled connection cannot be the default");

            entries.Add(CreateEntry(file, source, name, id, code));
        }

        var defaultCount = entries.Count(static entry =>
            entry.Descriptor.Enabled && entry.Descriptor.IsDefault);
        if (defaultCount != 1)
        {
            throw new InvalidOperationException(
                $"Felina Storage requires exactly one enabled default connection, but found {defaultCount} in '{directory}'.");
        }

        return entries
            .OrderBy(static entry => entry.Descriptor.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static StorageConnectionFile Deserialize(string file)
    {
        try
        {
            return JsonSerializer.Deserialize<StorageConnectionFile>(File.ReadAllText(file), JsonOptions)
                ?? throw Invalid(file, "the JSON document is empty");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Felina Storage connection file '{Path.GetFileName(file)}' contains invalid JSON.",
                ex);
        }
    }

    private static StorageClientRegistrationEntry CreateEntry(
        string file,
        StorageConnectionFile source,
        string name,
        string id,
        string code)
    {
        var baseUrl = source.BaseUrl?.Trim() ?? string.Empty;
        var basePath = NormalizeDisplayPath(source.BasePath);
        var clientId = NormalizeOptional(source.ClientId);
        var adminClientId = NormalizeOptional(source.AdminClientId);

        if (!source.Enabled)
        {
            return new StorageClientRegistrationEntry(
                new StorageClientDescriptor(
                    name,
                    id,
                    code,
                    false,
                    false,
                    baseUrl,
                    basePath,
                    clientId,
                    HasAnySecretSource(source),
                    adminClientId,
                    HasAnyAdminSecretSource(source),
                    source.TimeoutSeconds),
                null);
        }

        baseUrl = ValidateBaseUrl(file, baseUrl);
        basePath = ValidateBasePath(file, basePath);
        if (source.TimeoutSeconds < 0)
            throw Invalid(file, "timeoutSeconds cannot be negative");

        var serviceKey = ResolveServiceKey(file, source);
        var hasClientId = !string.IsNullOrWhiteSpace(clientId);
        var hasServiceKey = !string.IsNullOrWhiteSpace(serviceKey);
        if (hasClientId != hasServiceKey)
            throw Invalid(file, "clientId and one service-key source must either both be supplied or both be omitted");

        var adminKey = ResolveAdminKey(file, source);
        var hasAdminClientId = !string.IsNullOrWhiteSpace(adminClientId);
        var hasAdminKey = !string.IsNullOrWhiteSpace(adminKey);
        if (hasAdminClientId != hasAdminKey)
            throw Invalid(file, "adminClientId and one admin-key source must either both be supplied or both be omitted");

        var descriptor = new StorageClientDescriptor(
            name,
            id,
            code,
            true,
            source.IsDefault,
            baseUrl,
            basePath,
            clientId,
            hasServiceKey,
            adminClientId,
            hasAdminKey,
            source.TimeoutSeconds);
        var options = new StorageClientOptions
        {
            Url = $"base={baseUrl};suffix={basePath}/;",
            TimeoutSeconds = source.TimeoutSeconds,
            Credential = new StorageClientCredentialOptions
            {
                Required = hasServiceKey,
                Id = clientId ?? string.Empty,
                Secret = serviceKey ?? string.Empty,
                ClientHeader = StorageClientHeaders.Client,
                KeyHeader = StorageClientHeaders.Key
            },
            AdminCredential = new StorageClientCredentialOptions
            {
                Required = hasAdminKey,
                Id = adminClientId ?? string.Empty,
                Secret = adminKey ?? string.Empty,
                ClientHeader = StorageClientHeaders.AdminClient,
                KeyHeader = StorageClientHeaders.AdminKey
            }
        };

        return new StorageClientRegistrationEntry(descriptor, options);
    }

    private static string ValidateBaseUrl(string file, string value)
    {
        if (value.Contains(';', StringComparison.Ordinal) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw Invalid(file, "baseUrl must be an absolute HTTP or HTTPS URL without a query or fragment");
        }

        return value.EndsWith("/", StringComparison.Ordinal) ? value : value + '/';
    }

    private static string ValidateBasePath(string file, string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Contains(';', StringComparison.Ordinal) ||
            value.Contains('?', StringComparison.Ordinal) ||
            value.Contains('#', StringComparison.Ordinal) ||
            Uri.TryCreate(value, UriKind.Absolute, out _) ||
            value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(static segment => segment is "." or ".."))
        {
            throw Invalid(file, "basePath must be a non-empty relative URL path");
        }

        return value;
    }

    private static string? ResolveServiceKey(string file, StorageConnectionFile source) =>
        ResolveSecret(file, "service key", source.ServiceKey, source.ServiceKeyEnv, source.ServiceKeyFile);

    private static string? ResolveAdminKey(string file, StorageConnectionFile source) =>
        ResolveSecret(file, "admin key", source.AdminKey, source.AdminKeyEnv, source.AdminKeyFile);

    private static string? ResolveSecret(
        string file,
        string label,
        string? inlineValue,
        string? environmentVariable,
        string? secretFilePath)
    {
        var sources = new[]
        {
            NormalizeOptional(inlineValue),
            NormalizeOptional(environmentVariable),
            NormalizeOptional(secretFilePath)
        };
        if (sources.Count(static value => value is not null) > 1)
            throw Invalid(file, $"only one {label} source may be supplied");

        if (sources[0] is not null)
            return sources[0];

        if (sources[1] is not null)
        {
            var environmentName = sources[1]!;
            var value = Environment.GetEnvironmentVariable(environmentName);
            if (string.IsNullOrWhiteSpace(value))
                throw Invalid(file, $"environment variable '{environmentName}' does not contain an {label}");
            return value;
        }

        if (sources[2] is null)
            return null;
        var keyFile = sources[2]!;
        if (!Path.IsPathRooted(keyFile))
            throw Invalid(file, $"the {label} file path must be absolute");
        if (!File.Exists(keyFile))
            throw Invalid(file, $"the {label} file '{keyFile}' does not exist");

        var secret = File.ReadAllText(keyFile).TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(secret))
            throw Invalid(file, $"the {label} file is empty");
        return secret;
    }

    private static bool HasAnySecretSource(StorageConnectionFile source) =>
        !string.IsNullOrWhiteSpace(source.ServiceKey) ||
        !string.IsNullOrWhiteSpace(source.ServiceKeyEnv) ||
        !string.IsNullOrWhiteSpace(source.ServiceKeyFile);

    private static bool HasAnyAdminSecretSource(StorageConnectionFile source) =>
        !string.IsNullOrWhiteSpace(source.AdminKey) ||
        !string.IsNullOrWhiteSpace(source.AdminKeyEnv) ||
        !string.IsNullOrWhiteSpace(source.AdminKeyFile);

    private static string NormalizeDisplayPath(string? value) =>
        (value ?? string.Empty).Trim().Replace('\\', '/').Trim('/');

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static InvalidOperationException Invalid(string file, string reason) =>
        new($"Felina Storage connection file '{Path.GetFileName(file)}' is invalid: {reason}.");
}
