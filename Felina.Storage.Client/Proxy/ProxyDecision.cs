using Microsoft.AspNetCore.Http;

namespace Felina.Client;

public sealed class ProxyDecision
{
    public bool Allowed { get; init; } = true;
    public int StatusCode { get; init; } = StatusCodes.Status200OK;
    public string? Message { get; init; }
    public string? StoragePath { get; init; }
    public Dictionary<string, string?> Query { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static ProxyDecision Allow() => new();

    public static ProxyDecision Deny(int statusCode, string message) =>
        new() { Allowed = false, StatusCode = statusCode, Message = message };
}
