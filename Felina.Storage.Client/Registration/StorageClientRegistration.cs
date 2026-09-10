using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Haley.Utils;

namespace Felina.Client;

public static class StorageClientRegistration
{
    public static IServiceCollection AddStorageClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = StorageClientOptions.DefaultSectionName,
        Action<StorageClientOptions>? configure = null)
        => AddStorageClient(services, configuration, Options.DefaultName, sectionName, configure, true);

    public static IServiceCollection AddStorageClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string name,
        string sectionName,
        Action<StorageClientOptions>? configure = null)
        => AddStorageClient(services, configuration, name, sectionName, configure, false);

    public static IServiceCollection AddStorageClients(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Felina:Clients",
        string defaultsSectionName = "Felina:Defaults")
    {
        var clients = configuration.GetSection(sectionName).GetChildren().ToArray();
        if (clients.Length == 0)
            throw new InvalidOperationException($"No named Felina Storage clients were found under '{sectionName}'.");

        foreach (var client in clients)
            AddStorageClient(
                services,
                configuration,
                client.Key,
                client.Path,
                null,
                false,
                defaultsSectionName);

        return services;
    }

    private static IServiceCollection AddStorageClient(
        IServiceCollection services,
        IConfiguration configuration,
        string name,
        string sectionName,
        Action<StorageClientOptions>? configure,
        bool exposeDefaultClient,
        string? defaultsSectionName = null)
    {
        if (string.IsNullOrWhiteSpace(name) && name != Options.DefaultName)
            throw new ArgumentException("A Felina Storage client name is required.", nameof(name));

        var options = services.AddOptions<StorageClientOptions>(name);
        if (!string.IsNullOrWhiteSpace(defaultsSectionName))
            options.Bind(configuration.GetSection(defaultsSectionName));
        options.Bind(configuration.GetSection(sectionName))
            .Validate(static value =>
                    IsValidEndpointDescriptor(value.Url) &&
                    value.TimeoutSeconds >= 0 &&
                    (!value.Credential.Required ||
                        (!string.IsNullOrWhiteSpace(value.Credential.Id) &&
                         !string.IsNullOrWhiteSpace(value.Credential.Secret))) &&
                    !string.IsNullOrWhiteSpace(value.Credential.ClientHeader) &&
                    !string.IsNullOrWhiteSpace(value.Credential.KeyHeader),
                "Felina Storage Url must be a valid Haley endpoint descriptor. Credential Id and Secret are required when credential authentication is enabled.");

        if (configure is not null)
            options.Configure(configure);

        options.ValidateOnStart();

        services.AddOptions<SignedViewOptions>(name)
            .Bind(configuration.GetSection($"{sectionName}:SignedView"));

        services.AddSingleton(new StorageClientRegistrationEntry(name));
        services.TryAddSingleton<IStorageClientFactory, StorageClientFactory>();
        if (exposeDefaultClient)
        {
            services.TryAddSingleton<IStorageClient>(static provider =>
                provider.GetRequiredService<IStorageClientFactory>().GetRequiredClient(Options.DefaultName));
        }
        services.TryAddSingleton<ISignedViewTokenService, SignedViewTokenService>();
        return services;
    }

    private static bool IsValidEndpointDescriptor(string value)
    {
        try
        {
            return Uri.TryCreate(value.ToDictionarySplit().GenerateBaseURLAddress(), UriKind.Absolute, out _);
        }
        catch
        {
            return false;
        }
    }
}
