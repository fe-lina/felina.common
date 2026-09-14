using System.Net;

namespace Felina.Client;

/// <summary>Single-send HTTP content that borrows, rather than owns, the upload stream.</summary>
internal sealed class UploadStreamContent : HttpContent
{
    private readonly Stream _source;
    private readonly long? _length;
    private int _sent;

    public UploadStreamContent(Stream source)
    {
        _source = source;
        _length = source.CanSeek ? source.Length - source.Position : null;
        if (_length < 0)
            throw new ArgumentException("Upload stream position is beyond its length.", nameof(source));
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _length.GetValueOrDefault();
        return _length.HasValue;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override Task SerializeToStreamAsync(
        Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        // Do not silently replay a non-idempotent upload after a redirect/authentication challenge.
        if (Interlocked.Exchange(ref _sent, 1) != 0)
            throw new InvalidOperationException("An upload body cannot be sent more than once.");

        return _source.CopyToAsync(stream, 128 * 1024, cancellationToken);
    }
}
