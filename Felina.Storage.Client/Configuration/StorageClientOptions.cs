namespace Felina.Client;

public sealed class StorageClientOptions
{
    public string Url { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 300;
    public StorageClientCredentialOptions Credential { get; set; } = new();
    public StorageClientCredentialOptions AdminCredential { get; set; } = new()
    {
        Required = false,
        ClientHeader = StorageClientHeaders.AdminClient,
        KeyHeader = StorageClientHeaders.AdminKey
    };
}
