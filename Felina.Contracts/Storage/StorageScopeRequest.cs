namespace Felina.Contracts;

public class StorageScopeRequest
{
    public string Client { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Workspace { get; set; }
}
