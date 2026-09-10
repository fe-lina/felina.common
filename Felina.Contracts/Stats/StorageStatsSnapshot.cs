namespace Felina.Contracts;

public sealed class StorageStatsSnapshot
{
    public StorageStatsNodeType NodeType { get; set; }
    public long NodeId { get; set; }
    public long WorkspaceId { get; set; }
    public string WorkspaceCuid { get; set; } = string.Empty;
    public long DirectoryId { get; set; }
    public string DirectoryCuid { get; set; } = string.Empty;
    public string DirectoryName { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    public StorageStatsCounters Direct { get; set; } = new();
    public StorageStatsCounters Recursive { get; set; } = new();
    public List<StorageExtensionStats> Extensions { get; set; } = new();
    public DateTimeOffset Generated { get; set; } = DateTimeOffset.UtcNow;
}
