namespace Felina.Contracts;

public class FolderTargetRequest : StorageScopeRequest
{
    public long? FolderId { get; set; }
    public string? FolderCuid { get; set; }
    public string? FolderName { get; set; }
}
