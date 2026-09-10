namespace Felina.Client;

public sealed class StorageClientCredentialOptions
{
    public bool Required { get; set; } = true;
    public string Id { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string ClientHeader { get; set; } = StorageClientHeaders.Client;
    public string KeyHeader { get; set; } = StorageClientHeaders.Key;
}
