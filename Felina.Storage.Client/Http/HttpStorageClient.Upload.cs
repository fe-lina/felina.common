using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Felina.Contracts;
using Haley.Abstractions;
using Haley.Models;

namespace Felina.Client;

internal sealed partial class HttpStorageClient
{
    public async Task<IFeedback<UploadedFile>> UploadAsync(
        UploadRequest request,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        ValidateScope(request);
        if (!content.CanRead)
            throw new ArgumentException("Upload stream must be readable.", nameof(content));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);
        if (request.FileName is "." or ".." || request.FileName.IndexOfAny(['/', '\\', '\r', '\n', '\0']) >= 0)
            throw new ArgumentException("FileName must be a file name, not a path or header value.", nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ContentType);
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType)
            || mediaType.MediaType?.Contains('*') != false)
            throw new ArgumentException("ContentType must be a concrete MIME type.", nameof(request));

        var versionUid = ValidateUploadUid(request.VersionUid, nameof(request.VersionUid));
        var rootUid = ValidateUploadUid(request.RootUid, nameof(request.RootUid));
        if (versionUid is not null && rootUid is not null)
            throw new ArgumentException("Provide VersionUid or RootUid, not both.", nameof(request));
        if (request.Thumbnail && versionUid is null && rootUid is null)
            throw new ArgumentException("Thumbnail upload requires a valid VersionUid or RootUid.", nameof(request));
        cancellationToken.ThrowIfCancellationRequested();

        var query = BuildFolderTargetQuery(request);
        query.Add(new("replace", request.Replace ? "true" : "false"));
        query.Add(new("thumb", request.Thumbnail ? "true" : "false"));
        if (request.Actor.HasValue)
            query.Add(new("actor", request.Actor.Value.ToString(CultureInfo.InvariantCulture)));

        using var message = new HttpRequestMessage(HttpMethod.Post, BuildUri("file", query));
        using var multipart = new MultipartFormDataContent();
        const string fileKey = "upload";
        // The data field and file share a disposition key; selectors are never query parameters.
        if (versionUid is not null || rootUid is not null)
            multipart.Add(new StringContent(rootUid is null ? versionUid! : $"{rootUid},root"), fileKey);
        using var fileContent = new UploadStreamContent(content);
        fileContent.Headers.ContentType = mediaType;
        multipart.Add(fileContent, fileKey, request.FileName);
        message.Content = multipart;
        AddServiceHeaders(message);

        var restResponse = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        using var response = restResponse.OriginalResponse
            ?? throw new HttpRequestException("Felina Storage returned an empty HTTP response.");
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(body, null, response.StatusCode);

        return ParseUploadResponse(body, request.Thumbnail);
    }

    private static string? ValidateUploadUid(string? value, string parameterName)
    {
        if (value is null) return null;
        if (!Guid.TryParse(value, out var uid) || uid == Guid.Empty)
            throw new ArgumentException("An explicitly supplied upload UID must be a non-empty GUID.", parameterName);
        return uid.ToString("N");
    }

    private static IFeedback<UploadedFile> ParseUploadResponse(string body, bool thumbnail)
    {
        MultipartReply? summary;
        try
        {
            summary = JsonSerializer.Deserialize<MultipartReply>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Felina Storage returned an invalid multipart upload response.", ex);
        }

        if (summary is null || summary.Passed is null || summary.Failed is null
            || summary.Passed < 0 || summary.Failed < 0
            || summary.Passed != (summary.PassedObjects?.Count ?? 0)
            || summary.Failed != (summary.FailedObjects?.Count ?? 0)
            || summary.Passed + summary.Failed > 1)
            throw new InvalidDataException("Felina Storage returned an incomplete or inconsistent multipart upload response.");

        var uploaded = summary.PassedObjects?.SingleOrDefault();
        if (!summary.Status || summary.Failed != 0 || uploaded?.Status != true)
        {
            IFeedbackBase failure = summary.FailedObjects?.FirstOrDefault() ?? (IFeedbackBase?)uploaded ?? summary;
            return new Feedback<UploadedFile>(false)
            {
                Message = !string.IsNullOrWhiteSpace(failure.Message) ? failure.Message
                    : !string.IsNullOrWhiteSpace(summary.Message) ? summary.Message : "No file was successfully uploaded.",
                Code = failure.Code,
                Key = failure.Key,
                Source = failure.Source
            };
        }

        var versionCuid = !string.IsNullOrWhiteSpace(uploaded.VersionCuid)
            ? uploaded.VersionCuid
            : uploaded.Result?.Cuid;
        var rootCuid = !string.IsNullOrWhiteSpace(uploaded.RootCuid)
            ? uploaded.RootCuid
            : uploaded.Result?.RootCuid;

        if (uploaded.Size is null || uploaded.Size < 0 || string.IsNullOrWhiteSpace(uploaded.OriginalName)
            || (!thumbnail && (!Guid.TryParse(versionCuid, out var uid) || uid == Guid.Empty
                || !Guid.TryParse(rootCuid, out var root) || root == Guid.Empty)))
            throw new InvalidDataException("Felina Storage returned upload success without the expected file identity or size.");

        return new Feedback<UploadedFile>(true)
        {
            Message = uploaded.Message,
            Code = uploaded.Code,
            Key = uploaded.Key,
            Source = uploaded.Source,
            Result = new UploadedFile
            {
                OriginalName = uploaded.OriginalName,
                Size = uploaded.Size.Value,
                VersionUid = thumbnail ? null : versionCuid,
                RootUid = thumbnail ? null : rootCuid,
                Version = uploaded.Result?.Version,
                Actor = uploaded.Result?.Actor
            }
        };
    }

    // Private wire shapes are specific to the Host's existing multipart summary.
    private sealed class MultipartReply : Feedback
    {
        public int? Passed { get; set; }
        public int? Failed { get; set; }
        public List<FileReply>? PassedObjects { get; set; }
        public List<FileReply>? FailedObjects { get; set; }
    }

    private sealed class FileReply : Feedback<VersionReply>
    {
        public string? OriginalName { get; set; }
        public long? Size { get; set; }
        public string? VersionCuid { get; set; }
        public string? RootCuid { get; set; }
    }

    private sealed class VersionReply
    {
        public int? Version { get; set; }
        public long? Actor { get; set; }
        public string? Cuid { get; set; }
        public string? RootCuid { get; set; }
    }
}
