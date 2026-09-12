using System.Text.Json.Serialization;

namespace Felina.Client;

internal sealed class StorageConnectionFile
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("default")]
    public bool IsDefault { get; set; }

    public string BaseUrl { get; set; } = string.Empty;
    public string BasePath { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ServiceKey { get; set; } = string.Empty;
    public string ServiceKeyEnv { get; set; } = string.Empty;
    public string ServiceKeyFile { get; set; } = string.Empty;
    public string AdminClientId { get; set; } = string.Empty;
    public string AdminKey { get; set; } = string.Empty;
    public string AdminKeyEnv { get; set; } = string.Empty;
    public string AdminKeyFile { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
}
