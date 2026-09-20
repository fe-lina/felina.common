using Microsoft.AspNetCore.Http;
using Felina.Contracts;
using Haley.Abstractions;

namespace Felina.Client;

public interface IStorageClient
{
    Task<FolderInfoResponse> GetFolderInfoAsync(FolderTargetRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task SetFolderMetadataAsync(FolderTargetRequest request, string metadata, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task RenameFolderAsync(RenameFolderRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task<FolderInfoResponse> UploadFolderThumbnailAsync(FolderThumbnailUploadRequest request, Stream content, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task<StorageFileResponse> OpenFolderThumbnailAsync(FolderTargetRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task<FolderInfoResponse> DeleteFolderThumbnailAsync(FolderThumbnailDeleteRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    /// <summary>
    /// Streams one file as multipart content from its current position without closing it.
    /// Check feedback.Status before using feedback.Result. No automatic retry is performed.
    /// </summary>
    Task<IFeedback<UploadedFile>> UploadAsync(
        UploadRequest request,
        Stream content,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This storage client does not originate multipart uploads. Use a named HTTP storage client.");

    /// <summary>
    /// Opens a complete backend file stream through the download route by default.
    /// Dispose the returned Content stream. Partial responses are rejected; use ProxyAsync for browser ranges.
    /// </summary>
    Task<StorageFileResponse> OpenReadAsync(
        FileDetailsRequest request,
        bool download = true,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This storage client does not expose file content as a backend stream. Use a named HTTP storage client.");

    Uri BuildUri(string storagePath, IEnumerable<KeyValuePair<string, string?>>? query = null);

    Task ProxyAsync(
        HttpContext context,
        ProxyTarget target,
        CancellationToken cancellationToken = default);

    Task<string> GetStringAsync(
        string storagePath,
        IEnumerable<KeyValuePair<string, string?>>? query = null,
        CancellationToken cancellationToken = default);

    Task<StorageCapacitySnapshot> GetCapacityAsync(
        CancellationToken cancellationToken = default);

    Task<FileDetailsResponse> GetFileDetailsAsync(
        FileDetailsRequest request,
        CancellationToken cancellationToken = default);

    Task<ProcessedFileProbeResponse> ProbeProcessedFileAsync(
        ProcessedFileProbeRequest request,
        CancellationToken cancellationToken = default);

    Task<FolderBrowseResponse> BrowseFolderAsync(
        FolderBrowseRequest request,
        CancellationToken cancellationToken = default);

    Task<FolderBrowseResponse> SearchFolderAsync(
        FolderSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<StorageStatsSnapshot> GetStatsAsync(
        StorageStatsRequest request,
        CancellationToken cancellationToken = default);

    Task<StorageAggregateStatsResponse> GetAggregateStatsAsync(
        StorageAggregateStatsRequest request,
        CancellationToken cancellationToken = default);

    Task<FolderMutationResponse> CreateFolderAsync(
        CreateFolderRequest request,
        CancellationToken cancellationToken = default);

    Task<UploadTicketResponse> CreateUploadTicketAsync(
        UploadTicketCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<FolderMutationResponse> DeleteFolderAsync(
        FolderTargetRequest request,
        bool recursive = true,
        CancellationToken cancellationToken = default);

    Task<FolderMutationResponse> RestoreFolderAsync(
        FolderTargetRequest request,
        bool force = false,
        CancellationToken cancellationToken = default);

    Task<StorageMoveResult> MoveFileAsync(
        MoveFileRequest request,
        CancellationToken cancellationToken = default);

    Task<StorageMoveResult> MoveFolderAsync(
        MoveFolderRequest request,
        CancellationToken cancellationToken = default);

    Task<FileMutationResponse> DeleteFileAsync(
        FileDetailsRequest request,
        bool hard = false,
        CancellationToken cancellationToken = default);

    Task<FileMutationResponse> RestoreFileAsync(
        FileDetailsRequest request,
        bool force = false,
        CancellationToken cancellationToken = default);
}
