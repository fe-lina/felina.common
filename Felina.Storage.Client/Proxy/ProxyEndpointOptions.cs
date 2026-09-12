using Microsoft.AspNetCore.Http;

namespace Felina.Client;

public sealed class ProxyEndpointOptions
{
    public string? ConnectionName { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public bool CopyIncomingQuery { get; set; } = true;
    public long? MaxRequestBodyBytes { get; set; }
    public int? StorageMaxSizeMegabytes { get; set; }
    public string StorageMaxSizeMegabytesHeaderName { get; set; } = StorageClientHeaders.MaxSizeMegabytes;
    public bool RewriteLocationToIncomingPath { get; set; }
    public Dictionary<string, string?> Query { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Func<HttpContext, ValueTask<ProxyDecision>>? PrepareAsync { get; set; }
}
