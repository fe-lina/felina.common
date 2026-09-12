using System.Diagnostics.CodeAnalysis;

namespace Felina.Client;

/// <summary>
/// Resolves Felina Storage deployments by application-owned connection name.
/// A connection name is not Felina's client scope value.
/// </summary>
public interface IStorageClientRegistry
{
    IStorageClient GetRequired(string name);

    bool TryGet(
        string name,
        [NotNullWhen(true)] out IStorageClient? client);

    IStorageClient GetDefault();

    StorageClientDescriptor GetDescriptor(string nameOrIdOrCode);

    bool TryGetDescriptor(
        string nameOrIdOrCode,
        [NotNullWhen(true)] out StorageClientDescriptor? descriptor);

    IReadOnlyCollection<StorageClientDescriptor> List();
}
