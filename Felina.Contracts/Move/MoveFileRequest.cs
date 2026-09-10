namespace Felina.Contracts;

public sealed class MoveFileRequest
{
    public FileDetailsRequest Source { get; set; } = new();
    public FolderTargetRequest Target { get; set; } = new();
    public bool Rename { get; set; }
}
