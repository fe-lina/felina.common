namespace Felina.Contracts;

public sealed class FileDetailsRequest : StorageScopeRequest
{
    public string? VersionUid { get; set; }
    public string? RootUid { get; set; }
    public string? ProcessedName { get; set; }
}
