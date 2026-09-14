namespace Felina.Contracts;

/// <summary>One multipart file upload into an application-authorized storage scope.</summary>
public sealed class UploadRequest : FolderTargetRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? VersionUid { get; set; }
    public string? RootUid { get; set; }

    /// <summary>With a UID/RUID, replace the selected version; false creates a new version.</summary>
    public bool Replace { get; set; } = true;

    /// <summary>Add a thumbnail to the selected content version. Requires a UID or RUID.</summary>
    public bool Thumbnail { get; set; }
    public long? Actor { get; set; }
}
