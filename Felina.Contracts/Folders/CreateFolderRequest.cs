namespace Felina.Contracts;

public sealed class CreateFolderRequest : FolderTargetRequest
{
    public string Name { get; set; } = string.Empty;
    public long? Actor { get; set; }
}
