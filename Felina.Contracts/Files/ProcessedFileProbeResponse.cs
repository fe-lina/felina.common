namespace Felina.Contracts;

public sealed class ProcessedFileProbeResponse
{
    public bool Exists { get; set; }
    public string ProcessedName { get; set; } = string.Empty;
    public string StorageRef { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
}
