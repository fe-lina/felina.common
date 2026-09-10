using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felina.Client;

internal sealed class StorageClientFactory : IStorageClientFactory, IDisposable
{
    private readonly IOptionsMonitor<StorageClientOptions> _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<string, string> _registeredNames;
    private readonly ConcurrentDictionary<string, IStorageClient> _clients =
        new(StringComparer.OrdinalIgnoreCase);

    public StorageClientFactory(
        IOptionsMonitor<StorageClientOptions> options,
        ILoggerFactory loggerFactory,
        IEnumerable<StorageClientRegistrationEntry> registrations)
    {
        _options = options;
        _loggerFactory = loggerFactory;
        _registeredNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var registration in registrations)
            _registeredNames.TryAdd(registration.Name, registration.Name);
    }

    public IStorageClient GetRequiredClient(string name)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? Options.DefaultName : name.Trim();
        if (!_registeredNames.TryGetValue(normalized, out var registeredName))
            throw new InvalidOperationException($"Felina Storage client '{name}' is not registered.");

        return _clients.GetOrAdd(registeredName, key =>
        {
            var options = _options.Get(key);
            return new HttpStorageClient(
                options,
                _loggerFactory.CreateLogger($"Felina.Storage.Client.{DisplayName(key)}"),
                key);
        });
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values.OfType<IDisposable>())
            client.Dispose();
        _clients.Clear();
    }

    private static string DisplayName(string name) =>
        string.IsNullOrWhiteSpace(name) ? "Default" : name;
}
