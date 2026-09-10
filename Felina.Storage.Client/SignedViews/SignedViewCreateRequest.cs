namespace Felina.Client;

public sealed class SignedViewCreateRequest
{
    public string? Uid { get; set; }
    public string? Ruid { get; set; }
    public string? Parent { get; set; }
    public string? Kind { get; set; }
    public TimeSpan? ExpiresIn { get; set; }
    public Dictionary<string, string> Query { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
