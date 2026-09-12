namespace Felina.Contracts;

public sealed class FolderBrowseRequest : FolderTargetRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public bool IncludeTotals { get; set; } = true;
    public bool IncludeAll { get; set; }
    public FolderSortMode Sort { get; set; } = FolderSortMode.Id;
    public SortDirection Direction { get; set; } = SortDirection.Asc;
    public FolderItemKind Kind { get; set; } = FolderItemKind.Both;
}
