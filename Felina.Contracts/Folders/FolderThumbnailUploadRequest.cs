namespace Felina.Contracts;

public sealed class FolderThumbnailUploadRequest : FolderTargetRequest {
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long? Actor { get; set; }
}
