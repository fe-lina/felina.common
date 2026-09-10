namespace Felina.Contracts;

public sealed class UploadTicketResponse
{
    public string TicketId { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string Env { get; set; } = string.Empty;
}
