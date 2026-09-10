namespace Felina.Client;

public sealed class ProxyTarget
{
    public string StoragePath { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public IReadOnlyList<KeyValuePair<string, string?>> Query { get; init; } =
        Array.Empty<KeyValuePair<string, string?>>();
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();
    public bool RewriteLocationToIncomingPath { get; init; }
}
