namespace Felina.Contracts;

public sealed class FolderInfoResponse {
    public string FolderCuid { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public bool HasThumbnail { get; set; }
    public bool IsHidden { get; set; }
}
