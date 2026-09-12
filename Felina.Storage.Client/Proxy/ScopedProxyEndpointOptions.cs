namespace Felina.Client;

public sealed class ScopedProxyEndpointOptions
{
    private readonly HashSet<string> _allowedIncomingQuery = new(StringComparer.OrdinalIgnoreCase);

    public string? ConnectionName { get; set; }
    public bool CopyIncomingQuery { get; set; }
    public long? MaxRequestBodyBytes { get; set; }
    public int? StorageMaxSizeMegabytes { get; set; }
    public string StorageMaxSizeMegabytesHeaderName { get; set; } = StorageClientHeaders.MaxSizeMegabytes;
    public bool RewriteLocationToIncomingPath { get; set; }
    public IReadOnlyCollection<string> AllowedIncomingQuery => _allowedIncomingQuery;
    public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

    public ScopedProxyEndpointOptions AllowIncomingQuery(params string[] names)
    {
        foreach (var name in names)
        {
            if (!string.IsNullOrWhiteSpace(name))
                _allowedIncomingQuery.Add(name.Trim());
        }

        return this;
    }

    public ScopedProxyEndpointOptions DisallowIncomingQuery(params string[] names)
    {
        foreach (var name in names)
        {
            if (!string.IsNullOrWhiteSpace(name))
                _allowedIncomingQuery.Remove(name.Trim());
        }

        return this;
    }

    public ScopedProxyEndpointOptions ClearIncomingQuery()
    {
        _allowedIncomingQuery.Clear();
        return this;
    }
}
