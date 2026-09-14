using System.Net;
using Felina.Contracts;

namespace Felina.Client;

internal sealed partial class HttpStorageClient
{
    public async Task<StorageFileResponse> OpenReadAsync(
        FileDetailsRequest request,
        bool download = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScope(request);
        ValidateFileSelector(request);

        using var message = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUri(download ? "file" : "file/view", BuildFileDetailsQuery(request)));
        AddServiceHeaders(message);
        var restResponse = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        var response = restResponse.OriginalResponse
            ?? throw new HttpRequestException("Felina Storage returned an empty HTTP response.");

        try
        {
            if (!response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException(payload, null, response.StatusCode);
            }

            // This API exposes a complete backend stream, not a browser's media range.
            if (response.StatusCode == HttpStatusCode.PartialContent || response.Content.Headers.ContentRange is not null)
                throw new HttpRequestException(
                    "Felina Storage returned partial file content. OpenReadAsync requires a complete file; " +
                    "use download: true, or ProxyAsync for browser range streaming.", null, response.StatusCode);

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? request.VersionUid
                ?? request.RootUid
                ?? request.ProcessedName
                ?? "document";
            return new StorageFileResponse(
                new ResponseOwnedStream(stream, response),
                fileName,
                response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                response.Content.Headers.ContentLength);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private sealed class ResponseOwnedStream(Stream inner, HttpResponseMessage response) : Stream
    {
        private int _disposed;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => inner.Read(buffer);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => inner.Write(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.WriteAsync(buffer, cancellationToken);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.WriteAsync(buffer, offset, count, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                try { inner.Dispose(); }
                finally { response.Dispose(); }
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                try { await inner.DisposeAsync().ConfigureAwait(false); }
                finally { response.Dispose(); }
            }

            GC.SuppressFinalize(this);
        }
    }
}
