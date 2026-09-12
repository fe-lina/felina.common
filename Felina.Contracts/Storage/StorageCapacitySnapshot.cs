namespace Felina.Contracts;

public sealed class StorageCapacitySnapshot
{
    public bool Available { get; set; }
    public long TotalBytes { get; set; }
    public long UsedBytes { get; set; }
    public long FreeBytes { get; set; }
    public long AvailableBytes { get; set; }
    public double UsedPercent { get; set; }
    public DateTimeOffset Observed { get; set; }
    public string? Message { get; set; }
}
