namespace Felina.Client;

internal static class StoragePathPolicy
{
    internal static string ValidateAndNormalize(string storagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var path = storagePath.Trim().Replace('\\', '/').Trim('/');
        var routePath = Uri.UnescapeDataString(path).Split('?', '#')[0];
        var segments = routePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
            throw new ArgumentException("A storage path is required.", nameof(storagePath));
        if (segments.Any(static segment => segment is "." or ".."))
            throw new ArgumentException("Relative storage path traversal is not allowed.", nameof(storagePath));
        if (string.Equals(segments[0], "admin", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The Storage API admin control plane is not available through IStorageClient.", nameof(storagePath));

        return path;
    }
}
