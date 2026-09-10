namespace Felina.Client;

public sealed class SignedViewOptions
{
    public string SigningKey { get; set; } = string.Empty;
    public int DefaultExpiryMinutes { get; set; } = 30;
}
