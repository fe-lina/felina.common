namespace Felina.Contracts;

public sealed class FolderBrowseResponse
{
    public long WorkspaceId { get; set; }
    public string WorkspaceCuid { get; set; } = string.Empty;
    public bool IsRoot { get; set; }
    public long CurrentFolderId { get; set; }
    public string CurrentFolderCuid { get; set; } = string.Empty;
    public string CurrentFolderName { get; set; } = string.Empty;
    public string CurrentFolderPath { get; set; } = string.Empty;
    public long CurrentFolderParentId { get; set; }
    public bool IncludeAll { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalItems { get; set; }
    public long TotalFolders { get; set; }
    public long TotalFiles { get; set; }
    public List<FolderBrowseItem> Items { get; set; } = new();
}
