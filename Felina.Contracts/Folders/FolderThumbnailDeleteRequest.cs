namespace Felina.Contracts;

public sealed class FolderThumbnailDeleteRequest : FolderTargetRequest {
    /// <summary>Erase every stored version. Otherwise, soft-delete the latest version. Both clear the association.</summary>
    public bool Permanent { get; set; }
}
