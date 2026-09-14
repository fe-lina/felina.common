namespace Felina.Contracts;

/// <summary>Public file identity returned after a successful multipart upload.</summary>
public sealed class UploadedFile
{
    public string OriginalName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? VersionUid { get; set; }
    public string? RootUid { get; set; }
    public int? Version { get; set; }
    public long? Actor { get; set; }
}
