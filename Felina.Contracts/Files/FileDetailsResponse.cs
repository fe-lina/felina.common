namespace Felina.Contracts;

public sealed class FileDetailsResponse
{
    public long DocumentId { get; set; }
    public string DocumentCuid { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long DocumentActorId { get; set; }
    public long WorkspaceId { get; set; }
    public string WorkspaceCuid { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public long DirectoryId { get; set; }
    public string DirectoryCuid { get; set; } = string.Empty;
    public string DirectoryName { get; set; } = string.Empty;
    public long DirectoryActorId { get; set; }
    public long DirectoryParentId { get; set; }
    public int DeleteState { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? Deleted { get; set; }
    public int DocumentDeleteState { get; set; }
    public bool DocumentIsDeleted { get; set; }
    public DateTime? DocumentDeleted { get; set; }
    public int VersionCount { get; set; }
    public string DocumentMetadata { get; set; } = string.Empty;
    public bool HasThumbnail { get; set; }
    public List<FileVersionDetails> Versions { get; set; } = new();
}
