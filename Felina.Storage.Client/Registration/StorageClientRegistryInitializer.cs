using Microsoft.Extensions.Hosting;

namespace Felina.Client;

internal sealed class StorageClientRegistryInitializer(IStorageClientRegistry registry) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = registry.GetDefault();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
