namespace Felina.Contracts;

public sealed class ProcessedFileProbeRequest : StorageScopeRequest
{
    public string ProcessedName { get; set; } = string.Empty;
}
