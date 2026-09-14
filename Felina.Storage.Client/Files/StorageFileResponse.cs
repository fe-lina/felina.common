namespace Felina.Client;

public sealed class StorageFileResponse
{
    internal StorageFileResponse(
        Stream content,
        string fileName,
        string contentType,
        long? contentLength)
    {
        Content = content;
        FileName = fileName;
        ContentType = contentType;
        ContentLength = contentLength;
    }

    public Stream Content { get; }
    public string FileName { get; }
    public string ContentType { get; }
    public long? ContentLength { get; }
}
