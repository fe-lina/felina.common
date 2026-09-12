using Microsoft.AspNetCore.Http;
using Felina.Contracts;

namespace Felina.Client;

public interface IStorageClient
{
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
