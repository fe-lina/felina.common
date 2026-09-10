namespace Felina.Client;

public sealed class StorageClientOptions
{
    public const string DefaultSectionName = "Felina:Storage:Client";

    public string Url { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 300;
    public StorageClientCredentialOptions Credential { get; set; } = new();
}
