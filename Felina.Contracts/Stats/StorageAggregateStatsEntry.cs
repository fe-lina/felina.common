namespace Felina.Contracts;

public sealed record StorageAggregateStatsEntry(
    StorageAggregateScope Scope,
    string Client,
    string? Module,
    string? Workspace,
    string DisplayName,
    string Cuid,
    StorageStatsCounters Counters,
    DateTimeOffset? Modified);
