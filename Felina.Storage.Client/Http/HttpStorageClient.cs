using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using Felina.Contracts;
using Haley.Abstractions;
using Haley.Rest;
using Haley.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Felina.Client;

internal sealed class HttpStorageClient : IStorageClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Content-Length",
        "Host",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    private static readonly HashSet<string> SensitiveIncomingHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie"
    };

    private readonly IClient _client;
    private readonly Uri _baseUri;
    private readonly StorageClientOptions _options;

    public HttpStorageClient(StorageClientOptions options, ILogger logger, string registrationName)
    {
        _options = options;
        _baseUri = NormalizeBaseUri(_options.Url.ToDictionarySplit().GenerateBaseURLAddress());
        var storeKey = $"Felina.Storage::{registrationName}::{_baseUri}";
        _client = ClientStore.Get(storeKey)
            ?? ClientStore.AddClient(storeKey, _options.Url, logger)
            ?? throw new InvalidOperationException($"Haley ClientStore could not create Felina client '{registrationName}'.");
        _client.WithTimeOut(_options.TimeoutSeconds <= 0
            ? Timeout.InfiniteTimeSpan
            : TimeSpan.FromSeconds(_options.TimeoutSeconds));
    }

    public Uri BuildUri(string storagePath, IEnumerable<KeyValuePair<string, string?>>? query = null)
    {
        var path = StoragePathPolicy.ValidateAndNormalize(storagePath);

        var builder = new UriBuilder(new Uri(_baseUri, path));
        var queryText = BuildQuery(query);
        if (!string.IsNullOrWhiteSpace(queryText))
            builder.Query = queryText;

        return builder.Uri;
    }

    public async Task<string> GetStringAsync(
        string storagePath,
        IEnumerable<KeyValuePair<string, string?>>? query = null,
        CancellationToken cancellationToken = default)
        => await SendStringAsync(HttpMethod.Get, storagePath, query, cancellationToken).ConfigureAwait(false);

    public async Task<StorageCapacitySnapshot> GetCapacityAsync(
        CancellationToken cancellationToken = default)
    {
        var content = await GetStringAsync("capacity", cancellationToken: cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<StorageCapacitySnapshot>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty capacity response.");
    }

    private async Task<string> SendStringAsync(
        HttpMethod method,
        string storagePath,
        IEnumerable<KeyValuePair<string, string?>>? query = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, BuildUri(storagePath, query));
        AddServiceHeaders(request);
        var restResponse = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        using var response = restResponse.OriginalResponse
            ?? throw new HttpRequestException("Felina Storage returned an empty HTTP response.");

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(content, null, response.StatusCode);

        return content;
    }

    private async Task<string> SendJsonStringAsync<T>(
        HttpMethod method,
        string storagePath,
        T body,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, BuildUri(storagePath))
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        AddServiceHeaders(request);
        var restResponse = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        using var response = restResponse.OriginalResponse
            ?? throw new HttpRequestException("Felina Storage returned an empty HTTP response.");

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(content, null, response.StatusCode);

        return content;
    }

    public async Task<FileDetailsResponse> GetFileDetailsAsync(
        FileDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Client);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Module);

        var query = BuildFileDetailsQuery(request);
        var content = await GetStringAsync("file/details", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FileDetailsResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty file details response.");
    }

    public async Task<ProcessedFileProbeResponse> ProbeProcessedFileAsync(
        ProcessedFileProbeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScope(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProcessedName);

        var query = BuildScopeQuery(request);
        query.Add(new KeyValuePair<string, string?>("pn", request.ProcessedName));
        var content = await GetStringAsync("file/probe", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<ProcessedFileProbeResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty processed-file probe response.");
    }

    public async Task<FolderBrowseResponse> BrowseFolderAsync(
        FolderBrowseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScope(request);

        var query = BuildFolderBrowseQuery(request);
        var content = await GetStringAsync("folder/items", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FolderBrowseResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty folder browse response.");
    }

    public async Task<FolderBrowseResponse> SearchFolderAsync(
        FolderSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScope(request);
        if (string.IsNullOrWhiteSpace(request.SearchTerm) && string.IsNullOrWhiteSpace(request.Extension))
            throw new ArgumentException("Search requires q or extension.");

        var query = BuildFolderSearchQuery(request);
        var content = await GetStringAsync("folder/search", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FolderBrowseResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty folder search response.");
    }

    public async Task<StorageStatsSnapshot> GetStatsAsync(
        StorageStatsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScope(request);

        var query = BuildStatsQuery(request);
        var content = await GetStringAsync("stats", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<StorageStatsSnapshot>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty stats response.");
    }

    public async Task<StorageAggregateStatsResponse> GetAggregateStatsAsync(
        StorageAggregateStatsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Client);
        if (!string.IsNullOrWhiteSpace(request.Workspace) && string.IsNullOrWhiteSpace(request.Module))
            throw new ArgumentException("Module is required when workspace is supplied.", nameof(request));

        var query = new List<KeyValuePair<string, string?>>
        {
            new("c", request.Client),
            new("m", request.Module),
            new("w", request.Workspace)
        };
        var content = await GetStringAsync("stats/aggregate", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<StorageAggregateStatsResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty aggregate stats response.");
    }

    public async Task<FolderMutationResponse> CreateFolderAsync(
        CreateFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScope(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var query = BuildFolderTargetQuery(request);
        query.Add(new KeyValuePair<string, string?>("name", request.Name));
        if (request.Actor.HasValue)
            query.Add(new KeyValuePair<string, string?>("actor", request.Actor.Value.ToString(CultureInfo.InvariantCulture)));

        var content = await SendStringAsync(HttpMethod.Post, "folder", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FolderMutationResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty folder create response.");
    }

    public async Task<UploadTicketResponse> CreateUploadTicketAsync(
        UploadTicketCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScope(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);

        var protocol = request.Protocol.Trim().ToLowerInvariant();
        if (protocol is not ("form" or "chunk" or "tus"))
            throw new ArgumentException("Upload ticket protocol must be form, chunk, or tus.", nameof(request));

        var query = BuildFolderTargetQuery(request);
        query.Add(new KeyValuePair<string, string?>("fn", request.FileName));
        query.Add(new KeyValuePair<string, string?>("fdn", request.DisplayName));
        query.Add(new KeyValuePair<string, string?>("protocol", protocol));
        query.Add(new KeyValuePair<string, string?>("env", "primary"));
        if (request.Actor.HasValue)
            query.Add(new KeyValuePair<string, string?>("actor", request.Actor.Value.ToString(CultureInfo.InvariantCulture)));

        var content = await SendStringAsync(HttpMethod.Post, "upload/begin", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<UploadTicketResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty upload ticket response.");
    }

    public async Task<FolderMutationResponse> DeleteFolderAsync(
        FolderTargetRequest request,
        bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScope(request);
        ValidateFolderTarget(request);

        var query = BuildFolderTargetQuery(request);
        query.Add(new KeyValuePair<string, string?>("recursive", recursive.ToString()));
        var content = await SendStringAsync(HttpMethod.Delete, "folder", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FolderMutationResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty folder delete response.");
    }

    public async Task<FolderMutationResponse> RestoreFolderAsync(
        FolderTargetRequest request,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScope(request);
        ValidateFolderTarget(request);

        var query = BuildFolderTargetQuery(request);
        query.Add(new KeyValuePair<string, string?>("force", force.ToString()));
        var content = await SendStringAsync(HttpMethod.Post, "folder/restore", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FolderMutationResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty folder restore response.");
    }

    public async Task<StorageMoveResult> MoveFileAsync(
        MoveFileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Source);
        ArgumentNullException.ThrowIfNull(request.Target);
        ValidateFileSelector(request.Source);

        var content = await SendJsonStringAsync(HttpMethod.Post, "file/move", request, cancellationToken)
            .ConfigureAwait(false);
        return JsonSerializer.Deserialize<StorageMoveResult>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty file move response.");
    }

    public async Task<StorageMoveResult> MoveFolderAsync(
        MoveFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Source);
        ArgumentNullException.ThrowIfNull(request.Target);
        ValidateFolderTarget(request.Source);
        ValidateScope(request.Source);

        var content = await SendJsonStringAsync(HttpMethod.Post, "folder/move", request, cancellationToken)
            .ConfigureAwait(false);
        return JsonSerializer.Deserialize<StorageMoveResult>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty folder move response.");
    }

    public async Task<FileMutationResponse> DeleteFileAsync(
        FileDetailsRequest request,
        bool hard = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateFileSelector(request);

        var query = BuildFileDetailsQuery(request);
        query.Add(new KeyValuePair<string, string?>("hard", hard.ToString()));
        var content = await SendStringAsync(HttpMethod.Delete, "file", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FileMutationResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty file delete response.");
    }

    public async Task<FileMutationResponse> RestoreFileAsync(
        FileDetailsRequest request,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateFileSelector(request);

        var query = BuildFileDetailsQuery(request);
        query.Add(new KeyValuePair<string, string?>("force", force.ToString()));
        var content = await SendStringAsync(HttpMethod.Post, "file/restore", query, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FileMutationResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Felina Storage returned an empty file restore response.");
    }

    public async Task ProxyAsync(
        HttpContext context,
        ProxyTarget target,
        CancellationToken cancellationToken = default)
    {
        var method = string.IsNullOrWhiteSpace(target.Method)
            ? new HttpMethod(context.Request.Method)
            : new HttpMethod(target.Method);

        using var request = new HttpRequestMessage(method, BuildUri(target.StoragePath, target.Query))
        {
            Version = context.Request.Protocol.Contains("2", StringComparison.OrdinalIgnoreCase)
                ? System.Net.HttpVersion.Version20
                : System.Net.HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };

        if (CanHaveBody(method) &&
            (context.Request.ContentLength.GetValueOrDefault() > 0 ||
             context.Request.Headers.ContainsKey("Transfer-Encoding")))
        {
            request.Content = new StreamContent(context.Request.Body, bufferSize: 128 * 1024);
            if (context.Request.ContentLength.HasValue)
                request.Content.Headers.ContentLength = context.Request.ContentLength.Value;
        }

        CopyRequestHeaders(context, request);
        foreach (var header in target.Headers)
            AddHeader(request, header.Key, header.Value);

        AddServiceHeaders(request);

        var restResponse = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        using var response = restResponse.OriginalResponse
            ?? throw new HttpRequestException("Felina Storage returned an empty HTTP response.");

        context.Response.StatusCode = (int)response.StatusCode;
        CopyResponseHeaders(context, response);
        if (target.RewriteLocationToIncomingPath && response.Headers.Location is not null)
            RewriteLocation(context, response.Headers.Location);
        await response.Content.CopyToAsync(context.Response.Body, cancellationToken).ConfigureAwait(false);
    }

    private static Uri NormalizeBaseUri(string baseUrl)
    {
        var normalized = baseUrl.EndsWith("/", StringComparison.Ordinal)
            ? baseUrl
            : baseUrl + "/";
        return new Uri(normalized, UriKind.Absolute);
    }

    private static bool CanHaveBody(HttpMethod method) =>
        method != HttpMethod.Get &&
        method != HttpMethod.Head &&
        method != HttpMethod.Options &&
        method != HttpMethod.Trace;

    private void CopyRequestHeaders(HttpContext context, HttpRequestMessage request)
    {
        foreach (var header in context.Request.Headers)
        {
            if (ShouldSkipIncomingHeader(header.Key)) continue;
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    private bool ShouldSkipIncomingHeader(string header) =>
        HopByHopHeaders.Contains(header) ||
        SensitiveIncomingHeaders.Contains(header) ||
        string.Equals(header, _options.Credential.ClientHeader, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(header, _options.Credential.KeyHeader, StringComparison.OrdinalIgnoreCase);

    private static void CopyResponseHeaders(HttpContext context, HttpResponseMessage response)
    {
        foreach (var header in response.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key)) continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key)) continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        if (response.Content.Headers.ContentLength.HasValue)
            context.Response.ContentLength = response.Content.Headers.ContentLength.Value;
    }

    private static void RewriteLocation(HttpContext context, Uri upstreamLocation)
    {
        var upstreamPath = upstreamLocation.IsAbsoluteUri
            ? upstreamLocation.AbsolutePath
            : upstreamLocation.OriginalString.Split('?', 2)[0];
        var uploadId = upstreamPath.TrimEnd('/').Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(uploadId))
            return;

        context.Response.Headers.Location =
            $"{context.Request.PathBase}{context.Request.Path.Value?.TrimEnd('/')}/{Uri.EscapeDataString(uploadId)}";
    }

    private void AddServiceHeaders(HttpRequestMessage request)
    {
        if (!_options.Credential.Required) return;

        request.Headers.Remove(_options.Credential.ClientHeader);
        request.Headers.Remove(_options.Credential.KeyHeader);
        request.Headers.TryAddWithoutValidation(_options.Credential.ClientHeader, _options.Credential.Id);
        request.Headers.TryAddWithoutValidation(_options.Credential.KeyHeader, _options.Credential.Secret);
    }

    private static List<KeyValuePair<string, string?>> BuildFileDetailsQuery(FileDetailsRequest request)
    {
        ValidateFileSelector(request);
        var query = BuildScopeQuery(request);

        if (!string.IsNullOrWhiteSpace(request.VersionUid))
            query.Add(new KeyValuePair<string, string?>("uid", request.VersionUid));
        else if (!string.IsNullOrWhiteSpace(request.RootUid))
            query.Add(new KeyValuePair<string, string?>("ruid", request.RootUid));
        else if (!string.IsNullOrWhiteSpace(request.ProcessedName))
            query.Add(new KeyValuePair<string, string?>("pn", request.ProcessedName));
        else
            throw new ArgumentException("File details requires versionUid, rootUid, or processedName.");

        return query;
    }

    private static List<KeyValuePair<string, string?>> BuildFolderBrowseQuery(FolderBrowseRequest request)
    {
        var query = BuildFolderTargetQuery(request);
        query.Add(new KeyValuePair<string, string?>("p", Math.Max(1, request.Page).ToString(CultureInfo.InvariantCulture)));
        query.Add(new KeyValuePair<string, string?>("ps", Math.Clamp(request.PageSize, 1, 200).ToString(CultureInfo.InvariantCulture)));
        query.Add(new KeyValuePair<string, string?>("totals", request.IncludeTotals.ToString()));
        query.Add(new KeyValuePair<string, string?>("include_all", request.IncludeAll.ToString()));
        query.Add(new KeyValuePair<string, string?>("sort", request.Sort.ToString()));
        query.Add(new KeyValuePair<string, string?>("dir", request.Direction.ToString()));
        query.Add(new KeyValuePair<string, string?>("kind", request.Kind.ToString()));
        return query;
    }

    private static List<KeyValuePair<string, string?>> BuildFolderSearchQuery(FolderSearchRequest request)
    {
        var query = BuildFolderTargetQuery(request);
        query.Add(new KeyValuePair<string, string?>("q", request.SearchTerm ?? string.Empty));
        query.Add(new KeyValuePair<string, string?>("mode", request.SearchMode.ToString()));
        query.Add(new KeyValuePair<string, string?>("ext", request.Extension));
        query.Add(new KeyValuePair<string, string?>("recursive", request.Recursive.ToString()));
        query.Add(new KeyValuePair<string, string?>("p", Math.Max(1, request.Page).ToString(CultureInfo.InvariantCulture)));
        query.Add(new KeyValuePair<string, string?>("ps", Math.Clamp(request.PageSize, 1, 200).ToString(CultureInfo.InvariantCulture)));
        query.Add(new KeyValuePair<string, string?>("totals", request.IncludeTotals.ToString()));
        query.Add(new KeyValuePair<string, string?>("include_all", request.IncludeAll.ToString()));
        query.Add(new KeyValuePair<string, string?>("sort", request.Sort.ToString()));
        query.Add(new KeyValuePair<string, string?>("dir", request.Direction.ToString()));
        query.Add(new KeyValuePair<string, string?>("kind", request.Kind.ToString()));
        return query;
    }

    private static List<KeyValuePair<string, string?>> BuildStatsQuery(StorageStatsRequest request)
    {
        var query = BuildFolderTargetQuery(request);
        query.Add(new KeyValuePair<string, string?>("ext", request.Extension));
        return query;
    }

    private static List<KeyValuePair<string, string?>> BuildFolderTargetQuery(FolderTargetRequest request)
    {
        var query = BuildScopeQuery(request);
        if (request.FolderId.HasValue && request.FolderId.Value > 0)
            query.Add(new KeyValuePair<string, string?>("did", request.FolderId.Value.ToString(CultureInfo.InvariantCulture)));
        else if (!string.IsNullOrWhiteSpace(request.FolderCuid))
            query.Add(new KeyValuePair<string, string?>("duid", request.FolderCuid));
        else if (!string.IsNullOrWhiteSpace(request.FolderName))
            query.Add(new KeyValuePair<string, string?>("d", request.FolderName));
        return query;
    }

    private static List<KeyValuePair<string, string?>> BuildScopeQuery(StorageScopeRequest request)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("c", request.Client),
            new("m", request.Module)
        };

        if (!IsDefaultWorkspace(request.Workspace))
            query.Add(new KeyValuePair<string, string?>("w", request.Workspace));
        return query;
    }

    private static bool IsDefaultWorkspace(string? workspace)
        => string.IsNullOrWhiteSpace(workspace) ||
           string.Equals(workspace.Trim(), "default", StringComparison.OrdinalIgnoreCase);

    private static void ValidateScope(StorageScopeRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Client);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Module);
    }

    private static void ValidateFileSelector(FileDetailsRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Client);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Module);
        if (string.IsNullOrWhiteSpace(request.VersionUid) &&
            string.IsNullOrWhiteSpace(request.RootUid) &&
            string.IsNullOrWhiteSpace(request.ProcessedName))
        {
            throw new ArgumentException("File operation requires versionUid, rootUid, or processedName.");
        }
    }

    private static void ValidateFolderTarget(FolderTargetRequest request)
    {
        if (request.FolderId.GetValueOrDefault() <= 0 &&
            string.IsNullOrWhiteSpace(request.FolderCuid) &&
            string.IsNullOrWhiteSpace(request.FolderName))
        {
            throw new ArgumentException("Folder operation requires folderId, folderCuid, or folderName.");
        }
    }

    private static void AddHeader(HttpRequestMessage request, string name, string value)
    {
        if (HopByHopHeaders.Contains(name)) return;
        if (!request.Headers.TryAddWithoutValidation(name, value))
            request.Content?.Headers.TryAddWithoutValidation(name, value);
    }

    private static string BuildQuery(IEnumerable<KeyValuePair<string, string?>>? query)
    {
        if (query is null) return string.Empty;

        var parts = new List<string>();
        foreach (var item in query)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || item.Value is null) continue;
            parts.Add($"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}");
        }

        return string.Join("&", parts);
    }

}
