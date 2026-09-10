using Microsoft.AspNetCore.Http;

namespace Felina.Client;

public sealed class StorageProxyScopeDecision
{
    public bool Allowed { get; init; } = true;
    public int StatusCode { get; init; } = StatusCodes.Status200OK;
    public string? Message { get; init; }
    public string? Client { get; init; }
    public string? Module { get; init; }
    public string? Workspace { get; init; }
    public string? Folder { get; init; }
    public long? FolderId { get; init; }
    public string? FolderCuid { get; init; }
    public string? Actor { get; init; }
    public Dictionary<string, string?> Query { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static StorageProxyScopeDecision Allow(
        string client,
        string module,
        string? workspace = null,
        string? folder = null,
        string? actor = null)
        => new()
        {
            Client = client,
            Module = module,
            Workspace = workspace,
            Folder = folder,
            Actor = actor
        };

    public static StorageProxyScopeDecision Deny(int statusCode, string message)
        => new()
        {
            Allowed = false,
            StatusCode = statusCode,
            Message = message
        };
}
