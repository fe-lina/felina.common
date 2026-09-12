using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Felina.Client;

internal sealed class StorageClientRegistry : IStorageClientRegistry
{
    private readonly FrozenDictionary<string, IStorageClient> _clients;
    private readonly FrozenDictionary<string, StorageClientDescriptor> _descriptorsByAlias;
    private readonly IReadOnlyCollection<StorageClientDescriptor> _descriptors;
    private readonly string _defaultName;

    public StorageClientRegistry(
        IEnumerable<StorageClientRegistrationEntry> registrations,
        ILoggerFactory loggerFactory)
    {
        var entries = registrations.ToArray();
        _descriptors = Array.AsReadOnly(entries.Select(static entry => entry.Descriptor).ToArray());
        _defaultName = entries.Single(static entry =>
            entry.Descriptor.Enabled && entry.Descriptor.IsDefault).Descriptor.Name;

        _clients = entries
            .Where(static entry => entry.Descriptor.Enabled)
            .ToFrozenDictionary(
                static entry => entry.Descriptor.Name,
                entry => (IStorageClient)new HttpStorageClient(
                    entry.Options ?? throw new InvalidOperationException(
                        $"Felina Storage connection '{entry.Descriptor.Name}' has no runtime options."),
                    loggerFactory.CreateLogger($"Felina.Storage.Client.{entry.Descriptor.Name}"),
                    entry.Descriptor.Name),
                StringComparer.OrdinalIgnoreCase);

        _descriptorsByAlias = entries
            .SelectMany(static entry => new[]
            {
                new KeyValuePair<string, StorageClientDescriptor>(entry.Descriptor.Name, entry.Descriptor),
                new KeyValuePair<string, StorageClientDescriptor>(entry.Descriptor.Id, entry.Descriptor),
                new KeyValuePair<string, StorageClientDescriptor>(entry.Descriptor.Code, entry.Descriptor)
            })
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                static group => group.Key,
                static group => group.First().Value,
                StringComparer.OrdinalIgnoreCase);
    }

    public IStorageClient GetRequired(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (TryGet(name, out var client))
            return client;

        throw new InvalidOperationException($"Felina Storage connection '{name}' is not enabled or registered.");
    }

    public bool TryGet(
        string name,
        [NotNullWhen(true)] out IStorageClient? client)
    {
        client = null;
        return !string.IsNullOrWhiteSpace(name) &&
            _clients.TryGetValue(name.Trim(), out client);
    }

    public IStorageClient GetDefault() => GetRequired(_defaultName);

    public StorageClientDescriptor GetDescriptor(string nameOrIdOrCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameOrIdOrCode);
        if (TryGetDescriptor(nameOrIdOrCode, out var descriptor))
            return descriptor;

        throw new InvalidOperationException($"Felina Storage host '{nameOrIdOrCode}' is not registered.");
    }

    public bool TryGetDescriptor(
        string nameOrIdOrCode,
        [NotNullWhen(true)] out StorageClientDescriptor? descriptor)
    {
        descriptor = null;
        return !string.IsNullOrWhiteSpace(nameOrIdOrCode) &&
            _descriptorsByAlias.TryGetValue(nameOrIdOrCode.Trim(), out descriptor);
    }

    public IReadOnlyCollection<StorageClientDescriptor> List() => _descriptors;
}
