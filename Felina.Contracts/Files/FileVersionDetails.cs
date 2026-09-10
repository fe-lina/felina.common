namespace Felina.Contracts;

public sealed class FileVersionDetails
{
    public long VersionId { get; set; }
    public string VersionCuid { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public long ActorId { get; set; }
    public int DeleteState { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? Deleted { get; set; }
    public DateTime? Created { get; set; }
    public long? Size { get; set; }
    public string StorageName { get; set; } = string.Empty;
    public string StorageRef { get; set; } = string.Empty;
    public string StagingRef { get; set; } = string.Empty;
    public int Flags { get; set; }
    public string FlagsText { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTime? SyncedAt { get; set; }
    public string Metadata { get; set; } = string.Empty;
}
