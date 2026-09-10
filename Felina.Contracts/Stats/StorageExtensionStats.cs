namespace Felina.Contracts;

public sealed class StorageExtensionStats
{
    public string Extension { get; set; } = string.Empty;
    public StorageStatsCounters Direct { get; set; } = new();
    public StorageStatsCounters Recursive { get; set; } = new();
}
