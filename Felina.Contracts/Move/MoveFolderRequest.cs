namespace Felina.Contracts;

public sealed class MoveFolderRequest
{
    public FolderTargetRequest Source { get; set; } = new();
    public FolderTargetRequest Target { get; set; } = new();
    public bool Rename { get; set; }
}
