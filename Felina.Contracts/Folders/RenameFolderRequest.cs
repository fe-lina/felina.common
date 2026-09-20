namespace Felina.Contracts;

public sealed class RenameFolderRequest : FolderTargetRequest {
    public string Name { get; set; } = string.Empty;
}
