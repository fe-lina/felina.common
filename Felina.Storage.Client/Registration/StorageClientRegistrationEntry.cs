namespace Felina.Client;

internal sealed record StorageClientRegistrationEntry(
    StorageClientDescriptor Descriptor,
    StorageClientOptions? Options);
