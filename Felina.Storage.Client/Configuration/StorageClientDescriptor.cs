namespace Felina.Client;

public sealed record StorageClientDescriptor(
    string Name,
    string Id,
    string Code,
    bool Enabled,
    bool IsDefault,
    string BaseUrl,
    string BasePath,
    string? ClientId,
    bool UsesCredential,
    string? AdminClientId,
    bool UsesAdminCredential,
    int TimeoutSeconds);
