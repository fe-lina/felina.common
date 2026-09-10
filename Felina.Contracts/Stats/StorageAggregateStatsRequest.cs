namespace Felina.Contracts;

public sealed class StorageAggregateStatsRequest
{
    public string Client { get; set; } = string.Empty;
    public string? Module { get; set; }
    public string? Workspace { get; set; }
}
