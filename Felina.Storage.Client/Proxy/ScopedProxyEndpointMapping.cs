using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Felina.Client;

public static class ScopedProxyEndpointMapping
{
    private static readonly HashSet<string> ScopeQueryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "c",
        "m",
        "w",
        "d",
        "did",
        "duid",
        "actor"
    };

    public static RouteHandlerBuilder MapScopedStorageProxy(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string storagePath,
        string method,
        Func<HttpContext, StorageProxyScopeDecision> resolveScope,
        Action<ScopedProxyEndpointOptions>? configure = null)
        => endpoints.MapScopedStorageProxy(
            pattern,
            storagePath,
            method,
            context => ValueTask.FromResult(resolveScope(context)),
            configure);

    public static RouteHandlerBuilder MapScopedStorageProxy(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string storagePath,
        string method,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(resolveScope);

        var scopedOptions = new ScopedProxyEndpointOptions();
        configure?.Invoke(scopedOptions);

        return endpoints.MapStorageProxy(pattern, storagePath, method, options =>
        {
            options.ConnectionName = scopedOptions.ConnectionName;
            options.CopyIncomingQuery = false;
            options.MaxRequestBodyBytes = scopedOptions.MaxRequestBodyBytes;
            options.StorageMaxSizeMegabytes = scopedOptions.StorageMaxSizeMegabytes;
            options.StorageMaxSizeMegabytesHeaderName = scopedOptions.StorageMaxSizeMegabytesHeaderName;
            options.RewriteLocationToIncomingPath = scopedOptions.RewriteLocationToIncomingPath;
            foreach (var header in scopedOptions.Headers)
                options.Headers[header.Key] = header.Value;

            options.PrepareAsync = async context =>
            {
                var scope = await resolveScope(context).ConfigureAwait(false);
                return BuildProxyDecision(context, scope, scopedOptions);
            };
        });
    }

    public static MappedProxyEndpoints MapScopedStorageUploads(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, StorageProxyScopeDecision> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
        => endpoints.MapScopedStorageUploads(
            prefix,
            context => ValueTask.FromResult(resolveScope(context)),
            configureAll);

    public static MappedProxyEndpoints MapScopedStorageUploads(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
    {
        var mapped = CreateMappedGroup(endpoints, prefix);
        MapUploadRoutes(mapped, resolveScope, configureAll);
        return mapped;
    }

    public static MappedProxyEndpoints MapScopedStorageReads(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, StorageProxyScopeDecision> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
        => endpoints.MapScopedStorageReads(
            prefix,
            context => ValueTask.FromResult(resolveScope(context)),
            configureAll);

    public static MappedProxyEndpoints MapScopedStorageReads(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
    {
        var mapped = CreateMappedGroup(endpoints, prefix);
        MapReadRoutes(mapped, resolveScope, configureAll);
        return mapped;
    }

    public static MappedProxyEndpoints MapScopedStorageBrowse(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, StorageProxyScopeDecision> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
        => endpoints.MapScopedStorageBrowse(
            prefix,
            context => ValueTask.FromResult(resolveScope(context)),
            configureAll);

    public static MappedProxyEndpoints MapScopedStorageBrowse(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
    {
        var mapped = CreateMappedGroup(endpoints, prefix);
        MapBrowseRoutes(mapped, resolveScope, configureAll);
        return mapped;
    }

    public static MappedProxyEndpoints MapScopedStorageManagement(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, StorageProxyScopeDecision> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
        => endpoints.MapScopedStorageManagement(
            prefix,
            context => ValueTask.FromResult(resolveScope(context)),
            configureAll);

    public static MappedProxyEndpoints MapScopedStorageManagement(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
    {
        var mapped = CreateMappedGroup(endpoints, prefix);
        MapManagementRoutes(mapped, resolveScope, configureAll);
        return mapped;
    }

    public static MappedProxyEndpoints MapScopedStorageAll(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, StorageProxyScopeDecision> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
        => endpoints.MapScopedStorageAll(
            prefix,
            context => ValueTask.FromResult(resolveScope(context)),
            configureAll);

    public static MappedProxyEndpoints MapScopedStorageAll(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll = null)
    {
        var mapped = CreateMappedGroup(endpoints, prefix);
        MapUploadRoutes(mapped, resolveScope, configureAll);
        MapReadRoutes(mapped, resolveScope, configureAll);
        MapBrowseRoutes(mapped, resolveScope, configureAll);
        MapManagementRoutes(mapped, resolveScope, configureAll);
        return mapped;
    }

    private static MappedProxyEndpoints CreateMappedGroup(IEndpointRouteBuilder endpoints, string prefix)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return new MappedProxyEndpoints(endpoints.MapGroup(prefix));
    }

    private static void MapUploadRoutes(
        MappedProxyEndpoints mapped,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll)
    {
        mapped.Preflight = MapRoute(mapped.Group, "transfer/preflight", "transfer/preflight", HttpMethods.Post, resolveScope, configureAll, "protocol");
        mapped.Upload = MapRoute(mapped.Group, "file", "file", HttpMethods.Post, resolveScope, configureAll, "replace", "thumb");
        mapped.UploadBegin = MapRoute(mapped.Group, "upload/begin", "upload/begin", HttpMethods.Post, resolveScope, configureAll, "fn", "fdn", "protocol", "env");
        mapped.UploadTicket = MapRoute(mapped.Group, "upload/{ticketId}", "upload/{ticketId}", HttpMethods.Post, resolveScope, configureAll, "hash", "tostaging");
        mapped.UploadCommit = MapRoute(mapped.Group, "upload/commit", "upload/commit", HttpMethods.Post, resolveScope, configureAll, "uid", "size", "hash", "tostaging");
        mapped.UploadTicketUrl = MapRoute(mapped.Group, "upload/url", "upload/url", HttpMethods.Get, resolveScope, configureAll, "uid", "protocol", "env");
        mapped.UploadTicketChunkInit = MapRoute(mapped.Group, "upload/{ticketId}/chunk/init", "upload/{ticketId}/chunk/init", HttpMethods.Post, resolveScope, configureAll, "cs", "tp", "size");
        mapped.UploadTicketTusCreate = MapRoute(mapped.Group, "upload/{ticketId}/tus", "upload/{ticketId}/tus", HttpMethods.Post, resolveScope, configureAll);
        mapped.ChunkInit = MapRoute(mapped.Group, "chunk/init", "chunk/init", HttpMethods.Post, resolveScope, configureAll, "fn", "cs", "tp", "size");
        mapped.ChunkPart = MapRoute(mapped.Group, "chunk/part", "chunk/part", HttpMethods.Post, resolveScope, configureAll, "uid", "part", "hash");
        mapped.ChunkComplete = MapRoute(mapped.Group, "chunk/complete", "chunk/complete", HttpMethods.Post, resolveScope, configureAll, "uid", "hash");
        mapped.ChunkStatus = MapRoute(mapped.Group, "chunk/status", "chunk/status", HttpMethods.Get, resolveScope, configureAll, "uid");
        mapped.ChunkAbort = MapRoute(mapped.Group, "chunk", "chunk", HttpMethods.Delete, resolveScope, configureAll, "uid");
        mapped.TusOptions = MapRoute(mapped.Group, "tus", "tus", HttpMethods.Options, resolveScope, ConfigureTusCreate(configureAll));
        mapped.TusCreate = MapRoute(mapped.Group, "tus", "tus", HttpMethods.Post, resolveScope, ConfigureTusCreate(configureAll));
        mapped.TusHead = MapRoute(mapped.Group, "tus/{id}", "tus/{id}", HttpMethods.Head, resolveScope, configureAll);
        mapped.TusPatch = MapRoute(mapped.Group, "tus/{id}", "tus/{id}", HttpMethods.Patch, resolveScope, configureAll);
        mapped.TusDelete = MapRoute(mapped.Group, "tus/{id}", "tus/{id}", HttpMethods.Delete, resolveScope, configureAll);
    }

    private static void MapReadRoutes(
        MappedProxyEndpoints mapped,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll)
    {
        mapped.Download = MapRoute(mapped.Group, "file", "file", HttpMethods.Get, resolveScope, configureAll, "uid", "ruid", "pn", "sn");
        mapped.View = MapRoute(mapped.Group, "file/view", "file/view", HttpMethods.Get, resolveScope, configureAll, "uid", "ruid", "pn", "thumb", "strict");
        mapped.AccessUrl = MapRoute(mapped.Group, "file/url", "file/url", HttpMethods.Get, resolveScope, configureAll, "uid", "ruid", "pn", "tn", "expiry");
        mapped.Details = MapRoute(mapped.Group, "file/details", "file/details", HttpMethods.Get, resolveScope, configureAll, "uid", "ruid", "pn");
        mapped.Parent = MapRoute(mapped.Group, "file/parent", "file/parent", HttpMethods.Get, resolveScope, configureAll, "uid", "ruid", "pn");
        mapped.Revisions = MapRoute(mapped.Group, "file/revisions", "file/revisions", HttpMethods.Get, resolveScope, configureAll, "uid", "ruid", "pn");
        mapped.Revision = MapRoute(mapped.Group, "file/revision", "file/revision", HttpMethods.Get, resolveScope, configureAll, "uid", "ruid", "pn", "rev");
        mapped.RevisionView = MapRoute(mapped.Group, "file/revision/view", "file/revision/view", HttpMethods.Get, resolveScope, configureAll, "uid", "ruid", "pn", "rev");
        mapped.VersionMetadataGet = MapRoute(mapped.Group, "file/meta", "file/meta", HttpMethods.Get, resolveScope, configureAll, "uid");
        mapped.VersionMetadataSet = MapRoute(mapped.Group, "file/meta", "file/meta", HttpMethods.Put, resolveScope, configureAll, "uid");
        mapped.DocumentMetadataGet = MapRoute(mapped.Group, "file/docmeta", "file/docmeta", HttpMethods.Get, resolveScope, configureAll, "ruid");
        mapped.DocumentMetadataSet = MapRoute(mapped.Group, "file/docmeta", "file/docmeta", HttpMethods.Put, resolveScope, configureAll, "ruid");
    }

    private static void MapBrowseRoutes(
        MappedProxyEndpoints mapped,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll)
    {
        mapped.Browse = MapRoute(mapped.Group, "folder/items", "folder/items", HttpMethods.Get, resolveScope, configureAll, "p", "ps", "include_all");
        mapped.Search = MapRoute(mapped.Group, "folder/search", "folder/search", HttpMethods.Get, resolveScope, configureAll, "q", "mode", "ext", "recursive", "p", "ps", "include_all");
    }

    private static void MapManagementRoutes(
        MappedProxyEndpoints mapped,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll)
    {
        mapped.CreateFolder = MapRoute(mapped.Group, "folder", "folder", HttpMethods.Post, resolveScope, configureAll, "name");
        mapped.DeleteFile = MapRoute(mapped.Group, "file", "file", HttpMethods.Delete, resolveScope, configureAll, "uid", "ruid", "pn");
        mapped.RestoreFile = MapRoute(mapped.Group, "file/restore", "file/restore", HttpMethods.Post, resolveScope, configureAll, "uid", "ruid", "pn", "force");
        mapped.DeleteFolder = MapRoute(mapped.Group, "folder", "folder", HttpMethods.Delete, resolveScope, configureAll, "recursive");
        mapped.RestoreFolder = MapRoute(mapped.Group, "folder/restore", "folder/restore", HttpMethods.Post, resolveScope, configureAll, "force");
    }

    private static RouteHandlerBuilder MapRoute(
        RouteGroupBuilder group,
        string pattern,
        string storagePath,
        string method,
        Func<HttpContext, ValueTask<StorageProxyScopeDecision>> resolveScope,
        Action<ScopedProxyEndpointOptions>? configureAll,
        params string[] allowedIncomingQuery)
        => group.MapScopedStorageProxy(
            pattern,
            storagePath,
            method,
            resolveScope,
            options =>
            {
                options.AllowIncomingQuery(allowedIncomingQuery);
                configureAll?.Invoke(options);
            });

    private static Action<ScopedProxyEndpointOptions> ConfigureTusCreate(Action<ScopedProxyEndpointOptions>? configureAll)
        => options =>
        {
            options.RewriteLocationToIncomingPath = true;
            configureAll?.Invoke(options);
        };

    private static ProxyDecision BuildProxyDecision(
        HttpContext context,
        StorageProxyScopeDecision scope,
        ScopedProxyEndpointOptions options)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (!scope.Allowed)
            return ProxyDecision.Deny(scope.StatusCode, scope.Message ?? "Request rejected.");

        if (string.IsNullOrWhiteSpace(scope.Client) && string.IsNullOrWhiteSpace(scope.Module))
            return ProxyDecision.Deny(StatusCodes.Status400BadRequest, "Storage client and module scope are required.");
        if (string.IsNullOrWhiteSpace(scope.Client))
            return ProxyDecision.Deny(StatusCodes.Status400BadRequest, "Storage client scope is required.");
        if (string.IsNullOrWhiteSpace(scope.Module))
            return ProxyDecision.Deny(StatusCodes.Status400BadRequest, "Storage module scope is required.");

        var decision = ProxyDecision.Allow();
        CopyIncomingQuery(context, decision.Query, options);
        ApplyOverrides(decision.Query, scope.Query);
        ApplyScope(decision.Query, scope);

        foreach (var header in scope.Headers)
            decision.Headers[header.Key] = header.Value;

        return decision;
    }

    private static void CopyIncomingQuery(
        HttpContext context,
        Dictionary<string, string?> target,
        ScopedProxyEndpointOptions options)
    {
        foreach (var item in context.Request.Query)
        {
            if (ScopeQueryNames.Contains(item.Key))
                continue;
            if (!options.CopyIncomingQuery && !options.AllowedIncomingQuery.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
                continue;

            target[item.Key] = item.Value.ToString();
        }
    }

    private static void ApplyScope(
        Dictionary<string, string?> query,
        StorageProxyScopeDecision scope)
    {
        query["c"] = scope.Client;
        query["m"] = scope.Module;
        ApplyOptional(query, "w", scope.Workspace);
        ApplyOptional(query, "d", scope.Folder);

        if (scope.FolderId.HasValue)
            query["did"] = scope.FolderId.Value.ToString(CultureInfo.InvariantCulture);
        ApplyOptional(query, "duid", scope.FolderCuid);
        ApplyOptional(query, "actor", scope.Actor);
    }

    private static void ApplyOverrides(
        Dictionary<string, string?> query,
        IReadOnlyDictionary<string, string?> overrides)
    {
        foreach (var item in overrides)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
                continue;
            query[item.Key] = item.Value;
        }
    }

    private static void ApplyOptional(Dictionary<string, string?> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            query[name] = value;
    }
}
