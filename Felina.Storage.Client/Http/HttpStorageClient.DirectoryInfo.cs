using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Felina.Contracts;

namespace Felina.Client;

internal sealed partial class HttpStorageClient {
    public async Task<FolderInfoResponse> GetFolderInfoAsync(FolderTargetRequest request, CancellationToken cancellationToken = default) {
        ValidateScope(request);
        var content = await GetStringAsync("folder/info", BuildFolderTargetQuery(request), cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FolderInfoResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Storage returned an empty directory response.");
    }

    public async Task SetFolderMetadataAsync(FolderTargetRequest request, string metadata, CancellationToken cancellationToken = default) {
        ValidateScope(request);
        using var message = new HttpRequestMessage(HttpMethod.Put, BuildUri("folder/meta", BuildFolderTargetQuery(request))) {
            Content = new StringContent(metadata ?? string.Empty, Encoding.UTF8, "text/plain")
        };
        await SendDirectoryRequestAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task RenameFolderAsync(RenameFolderRequest request, CancellationToken cancellationToken = default) {
        ValidateScope(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        await SendJsonStringAsync(HttpMethod.Patch, "folder", request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FolderInfoResponse> UploadFolderThumbnailAsync(FolderThumbnailUploadRequest request, Stream content, CancellationToken cancellationToken = default) {
        ValidateScope(request);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);
        if (request.FileName.IndexOfAny(['/', '\\', '\r', '\n', '\0']) >= 0)
            throw new ArgumentException("FileName must be a file name, not a path.");
        var query = BuildFolderTargetQuery(request);
        if (request.Actor.HasValue) query.Add(new("actor", request.Actor.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        using var message = new HttpRequestMessage(HttpMethod.Post, BuildUri("folder/thumbnail", query));
        using var form = new MultipartFormDataContent();
        using var file = new UploadStreamContent(content);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
        form.Add(file, "thumbnail", request.FileName);
        message.Content = form;
        var payload = await SendDirectoryRequestAsync(message, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FolderInfoResponse>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Storage returned an empty directory response.");
    }

    public async Task<FolderInfoResponse> DeleteFolderThumbnailAsync(FolderThumbnailDeleteRequest request, CancellationToken cancellationToken = default) {
        ValidateScope(request);
        var query = BuildFolderTargetQuery(request);
        query.Add(new("permanent", request.Permanent ? "true" : "false"));
        using var message = new HttpRequestMessage(HttpMethod.Delete, BuildUri("folder/thumbnail", query));
        var payload = await SendDirectoryRequestAsync(message, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FolderInfoResponse>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Storage returned an empty directory response.");
    }

    private async Task<string> SendDirectoryRequestAsync(HttpRequestMessage message, CancellationToken cancellationToken) {
        AddServiceHeaders(message);
        var sent = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        using var response = sent.OriginalResponse ?? throw new HttpRequestException("Storage returned an empty HTTP response.");
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(payload, null, response.StatusCode);
        return payload;
    }

    public async Task<StorageFileResponse> OpenFolderThumbnailAsync(FolderTargetRequest request, CancellationToken cancellationToken = default) {
        ValidateScope(request);
        using var message = new HttpRequestMessage(HttpMethod.Get, BuildUri("folder/thumbnail", BuildFolderTargetQuery(request)));
        AddServiceHeaders(message);
        var sent = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var response = sent.OriginalResponse ?? throw new HttpRequestException("Storage returned an empty HTTP response.");
        try {
            if (!response.IsSuccessStatusCode) throw new HttpRequestException(await response.Content.ReadAsStringAsync(cancellationToken), null, response.StatusCode);
            if (response.StatusCode == System.Net.HttpStatusCode.PartialContent || response.Content.Headers.ContentRange is not null)
                throw new HttpRequestException("Storage returned a partial directory thumbnail.", null, response.StatusCode);
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return new StorageFileResponse(new ResponseOwnedStream(stream, response),
                response.Content.Headers.ContentDisposition?.FileNameStar ?? "directory-thumbnail",
                response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream", response.Content.Headers.ContentLength);
        } catch { response.Dispose(); throw; }
    }
}
