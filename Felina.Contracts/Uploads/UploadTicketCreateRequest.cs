namespace Felina.Contracts;

public sealed class UploadTicketCreateRequest : FolderTargetRequest
{
    public string FileName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Protocol { get; set; } = "form";
    public long? Actor { get; set; }
}
