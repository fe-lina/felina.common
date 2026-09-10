using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Felina.Client;

public static class ProxyEndpointMapping
{
    public static RouteHandlerBuilder MapStorageProxy(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string storagePath,
        string method,
        Action<ProxyEndpointOptions>? configure = null)
    {
        StoragePathPolicy.ValidateAndNormalize(storagePath);

        var options = new ProxyEndpointOptions
        {
            StoragePath = storagePath,
            Method = method
        };
        configure?.Invoke(options);

        return endpoints.MapMethods(
            pattern,
            [method],
            async (HttpContext context, IStorageClientFactory clients) =>
                await ExecuteAsync(
                    context,
                    clients.GetRequiredClient(options.ClientName),
                    options).ConfigureAwait(false));
    }

    public static MappedProxyEndpoints MapStorageProxyDefaults(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Action<ProxyEndpointOptions>? configureAll = null)
    {
        var group = endpoints.MapGroup(prefix);
        var mapped = new MappedProxyEndpoints(group)
        {
            Upload = group.MapStorageProxy("file", "file", HttpMethods.Post, configureAll),
            Download = group.MapStorageProxy("file", "file", HttpMethods.Get, configureAll),
            View = group.MapStorageProxy("file/view", "file/view", HttpMethods.Get, configureAll),
            Details = group.MapStorageProxy("file/details", "file/details", HttpMethods.Get, configureAll),
            Parent = group.MapStorageProxy("file/parent", "file/parent", HttpMethods.Get, configureAll),
            Browse = group.MapStorageProxy("folder/items", "folder/items", HttpMethods.Get, configureAll),
            Search = group.MapStorageProxy("folder/search", "folder/search", HttpMethods.Get, configureAll),
            CreateFolder = group.MapStorageProxy("folder", "folder", HttpMethods.Post, configureAll),
            DeleteFile = group.MapStorageProxy("file", "file", HttpMethods.Delete, configureAll),
            RestoreFile = group.MapStorageProxy("file/restore", "file/restore", HttpMethods.Post, configureAll),
            DeleteFolder = group.MapStorageProxy("folder", "folder", HttpMethods.Delete, configureAll),
            RestoreFolder = group.MapStorageProxy("folder/restore", "folder/restore", HttpMethods.Post, configureAll),
            ChunkInit = group.MapStorageProxy("chunk/init", "chunk/init", HttpMethods.Post, configureAll),
            ChunkPart = group.MapStorageProxy("chunk/part", "chunk/part", HttpMethods.Post, configureAll),
            ChunkComplete = group.MapStorageProxy("chunk/complete", "chunk/complete", HttpMethods.Post, configureAll),
            ChunkStatus = group.MapStorageProxy("chunk/status", "chunk/status", HttpMethods.Get, configureAll),
            ChunkAbort = group.MapStorageProxy("chunk", "chunk", HttpMethods.Delete, configureAll),
            TusOptions = group.MapStorageProxy("tus", "tus", HttpMethods.Options, configureAll),
            TusCreate = group.MapStorageProxy("tus", "tus", HttpMethods.Post, options =>
            {
                configureAll?.Invoke(options);
                options.RewriteLocationToIncomingPath = true;
            }),
            TusHead = group.MapStorageProxy("tus/{id}", "tus/{id}", HttpMethods.Head, configureAll),
            TusPatch = group.MapStorageProxy("tus/{id}", "tus/{id}", HttpMethods.Patch, configureAll),
            TusDelete = group.MapStorageProxy("tus/{id}", "tus/{id}", HttpMethods.Delete, configureAll)
        };

        return mapped;
    }

    private static async Task ExecuteAsync(
        HttpContext context,
        IStorageClient client,
        ProxyEndpointOptions options)
    {
        if (options.MaxRequestBodyBytes.HasValue &&
            context.Request.ContentLength.HasValue &&
            context.Request.ContentLength.Value > options.MaxRequestBodyBytes.Value)
        {
            await WriteMessageAsync(
                context,
                StatusCodes.Status413PayloadTooLarge,
                "Request body is larger than this endpoint allows.").ConfigureAwait(false);
            return;
        }

        var decision = options.PrepareAsync is null
            ? ProxyDecision.Allow()
            : await options.PrepareAsync(context).ConfigureAwait(false);

        if (!decision.Allowed)
        {
            await WriteMessageAsync(context, decision.StatusCode, decision.Message ?? "Request rejected.")
                .ConfigureAwait(false);
            return;
        }

        var query = BuildQuery(context, options, decision);
        var headers = new Dictionary<string, string>(options.Headers, StringComparer.OrdinalIgnoreCase);
        foreach (var item in decision.Headers)
            headers[item.Key] = item.Value;

        if (options.StorageMaxSizeMegabytes.HasValue
            && !string.IsNullOrWhiteSpace(options.StorageMaxSizeMegabytesHeaderName))
            headers[options.StorageMaxSizeMegabytesHeaderName] = options.StorageMaxSizeMegabytes.Value.ToString();

        await client.ProxyAsync(
            context,
            new ProxyTarget
            {
                StoragePath = ExpandRouteValues(context, decision.StoragePath ?? options.StoragePath),
                Method = options.Method,
                Query = query,
                Headers = headers,
                RewriteLocationToIncomingPath = options.RewriteLocationToIncomingPath
            },
            context.RequestAborted).ConfigureAwait(false);
    }

    private static string ExpandRouteValues(HttpContext context, string path)
    {
        foreach (var routeValue in context.Request.RouteValues)
        {
            path = path.Replace(
                $"{{{routeValue.Key}}}",
                Uri.EscapeDataString(Convert.ToString(routeValue.Value) ?? string.Empty),
                StringComparison.OrdinalIgnoreCase);
        }
        return path;
    }

    private static IReadOnlyList<KeyValuePair<string, string?>> BuildQuery(
        HttpContext context,
        ProxyEndpointOptions options,
        ProxyDecision decision)
    {
        var query = new List<KeyValuePair<string, string?>>();
        if (options.CopyIncomingQuery)
        {
            foreach (var item in context.Request.Query)
            {
                foreach (var value in item.Value)
                    query.Add(new KeyValuePair<string, string?>(item.Key, value));
            }
        }

        ApplyOverrides(query, options.Query);
        ApplyOverrides(query, decision.Query);
        return query;
    }

    private static void ApplyOverrides(
        List<KeyValuePair<string, string?>> query,
        IReadOnlyDictionary<string, string?> overrides)
    {
        foreach (var item in overrides)
        {
            query.RemoveAll(candidate =>
                string.Equals(candidate.Key, item.Key, StringComparison.OrdinalIgnoreCase));

            if (item.Value is not null)
                query.Add(new KeyValuePair<string, string?>(item.Key, item.Value));
        }
    }

    private static async Task WriteMessageAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(message).ConfigureAwait(false);
    }
}
