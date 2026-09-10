namespace Felina.Contracts;

public sealed class FolderMutationResponse
{
    public bool Deleted { get; set; }
    public bool Restored { get; set; }
    public bool Recursive { get; set; }
    public bool Force { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cuid { get; set; } = string.Empty;
    public long Actor { get; set; }
}
