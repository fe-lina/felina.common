using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Felina.Client;

public static class StorageClientRegistration
{
    public const string ConfigurationPathEnvironmentVariable = "FELINA_CONF_PATH";

    public static IServiceCollection AddStorageClientRegistry(this IServiceCollection services)
    {
        var configurationPath = Environment.GetEnvironmentVariable(ConfigurationPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            throw new InvalidOperationException(
                $"Environment variable '{ConfigurationPathEnvironmentVariable}' must identify the Felina Storage connection directory.");
        }

        return services.AddStorageClientRegistry(configurationPath);
    }

    public static IServiceCollection AddStorageClientRegistry(
        this IServiceCollection services,
        string configurationPath)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IStorageClientRegistry)))
            throw new InvalidOperationException("Felina Storage client registry is already registered.");

        var registrations = StorageClientConfigurationLoader.Load(configurationPath);
        foreach (var registration in registrations)
        {
            services.AddSingleton(registration);
            if (registration.Options is not null)
            {
                var source = registration.Options;
                services.AddOptions<StorageClientOptions>(registration.Descriptor.Name)
                    .Configure(options => Copy(source, options));
            }
        }

        services.AddSingleton<StorageClientRegistry>();
        services.AddSingleton<IStorageClientRegistry>(static provider =>
            provider.GetRequiredService<StorageClientRegistry>());
        services.AddHostedService<StorageClientRegistryInitializer>();
        services.AddOptions<SignedViewOptions>();
        services.TryAddSingleton<ISignedViewTokenService, SignedViewTokenService>();
        return services;
    }

    private static void Copy(StorageClientOptions source, StorageClientOptions target)
    {
        target.Url = source.Url;
        target.TimeoutSeconds = source.TimeoutSeconds;
        target.Credential = new StorageClientCredentialOptions
        {
            Required = source.Credential.Required,
            Id = source.Credential.Id,
            Secret = source.Credential.Secret,
            ClientHeader = source.Credential.ClientHeader,
            KeyHeader = source.Credential.KeyHeader
        };
        target.AdminCredential = new StorageClientCredentialOptions
        {
            Required = source.AdminCredential.Required,
            Id = source.AdminCredential.Id,
            Secret = source.AdminCredential.Secret,
            ClientHeader = source.AdminCredential.ClientHeader,
            KeyHeader = source.AdminCredential.KeyHeader
        };
    }
}
