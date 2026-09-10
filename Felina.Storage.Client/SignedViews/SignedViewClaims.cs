namespace Felina.Client;

public sealed class SignedViewClaims
{
    public string? Uid { get; set; }
    public string? Ruid { get; set; }
    public string? Parent { get; set; }
    public string? Kind { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Expires { get; set; }
    public Dictionary<string, string> Query { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
