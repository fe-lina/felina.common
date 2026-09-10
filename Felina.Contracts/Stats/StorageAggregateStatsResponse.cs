namespace Felina.Contracts;

public sealed record StorageAggregateStatsResponse(
    DateTimeOffset Generated,
    IReadOnlyList<StorageAggregateStatsEntry> Items);
