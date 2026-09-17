# Felina.Storage.Client

Reusable app-facing proxy helpers for services that keep Felina Storage internal.

Install only the client package in normal consuming APIs:

```xml
<PackageReference Include="Felina.Storage.Client" Version="0.0.1" />
```

`Felina.Contracts` is brought in transitively. Reference that smaller
package directly only when a project needs the storage DTOs but not the HTTP client
or endpoint-mapping helpers. The internal `Felina.Storage.Contracts` project is not a
public NuGet package.

`/api/va/admin/*` is deliberately not part of this package. `IStorageClient` exposes
only the storage endpoint surface, and its URL/proxy methods reject `admin` targets. Registry
mutation, runtime activation, startup persistence, and stats maintenance belong only to
the private Admin-to-Host management client.

The consuming API owns business authentication and authorization. This package only:

- forwards requests to internal Felina Storage
- uploads a backend-owned stream through a typed multipart call
- adds the backend service key headers
- maps reusable proxy endpoints when useful
- creates and validates signed view tokens

## Connection Registry

Keep deployment connections in a protected directory outside `wwwroot`. Set its path
through `FELINA_CONF_PATH`, or pass an explicit path for local tools and tests:

```csharp
using Felina.Client;

builder.Services.AddStorageClientRegistry();

public sealed class DocumentGateway(IStorageClientRegistry storageClients)
{
    public IStorageClient Primary => storageClients.GetDefault();
    public IStorageClient Archive => storageClients.GetRequired("archive");
}
```

```text
felina-conf/
  primary.json
  archive.json
  regional-dubai.json
```

Each file describes one Felina deployment:

```json
{
  "name": "primary",
  "id": "dxb-primary-storage",
  "code": "dxb1",
  "enabled": true,
  "default": true,
  "baseUrl": "http://felina-primary:5000",
  "basePath": "api/va",
  "clientId": "document-service-api",
  "serviceKeyEnv": "FELINA_PRIMARY_KEY",
  "timeoutSeconds": 120
}
```

Exactly one enabled connection must have `default: true`. `name`, `id`, and `code` are
case-insensitive unique aliases: `name` is the application lookup value, `id` is a
descriptive hyphenated Host slug, and `code` is a short alphanumeric routing value.
The registry rejects duplicate aliases. Names are case-insensitive,
disabled connections appear in `List()` but cannot be resolved, and files are loaded
once at startup. A connection name identifies a Felina deployment; it is not Felina's
`c` scope. The consuming application selects the deployment from trusted business
placement rules and must not accept the connection name from an untrusted browser.

Use one service-key source per connection: `serviceKey`, `serviceKeyEnv`, or an absolute
`serviceKeyFile`. Omit both `clientId` and the key when service authentication is not
enabled. Registry descriptors never expose the resolved key.

Admin may additionally configure `adminClientId` with exactly one of `adminKey`,
`adminKeyEnv`, or absolute `adminKeyFile`. These credentials are independent from the
ordinary service key and are not exposed through `IStorageClient`.

## Endpoint Mapping

Map routes one by one when each route needs its own policy:

```csharp
app.MapStorageProxy("/api/docs/file", "file", HttpMethods.Post, options =>
{
    options.ConnectionName = "primary";
    options.MaxRequestBodyBytes = 75 * 1024 * 1024;
    options.StorageMaxSizeMegabytes = 75;
    options.PrepareAsync = context =>
    {
        var decision = ProxyDecision.Allow();
        decision.Query["c"] = "sample";
        decision.Query["m"] = "documents";
        return ValueTask.FromResult(decision);
    };
}).RequireAuthorization("Manager");

app.MapStorageProxy("/api/docs/file/view", "file/view", HttpMethods.Get)
   .AllowAnonymous();
```

`StorageMaxSizeMegabytes` is forwarded as `X-Felina-Max-Size-MB` by default. Override
`StorageMaxSizeMegabytesHeaderName` only when proxying to an older storage Host that still
expects a different header.

Map the standard storage surface when one group-level policy is enough:

```csharp
var docs = app.MapStorageProxyDefaults("/api/docs");
docs.Group.RequireAuthorization("General");
```

The default mapping includes file and folder operations, the native chunk lifecycle,
and TUS discovery/create/resume/append/termination routes. It never maps Storage Host
admin routes. TUS create responses rewrite
the upstream `Location` header to the consuming API's public `/api/docs/tus/{id}` path.

## Typed Details Call

Use `IStorageClient.GetFileDetailsAsync(...)` when a backend service needs metadata without
proxying an HTTP request:

```csharp
var details = await storageClient.GetFileDetailsAsync(new FileDetailsRequest
{
    Client = "sample",
    Module = "documents",
    Workspace = "default",
    VersionUid = versionCuid
});
```

Use the same client to read physical capacity for the mounted filesystem containing
Felina's configured storage root. The response includes total, used, free, and
service-available bytes but never exposes the server path:

```csharp
var capacity = await storageClient.GetCapacityAsync();
```

## Backend File Stream

`OpenReadAsync` uses the complete-file download route by default. It returns after
response headers arrive, not after buffering the whole file:

```csharp
var file = await storage.OpenReadAsync(new FileDetailsRequest
{
    Client = clientName,
    Module = moduleName,
    VersionUid = versionUid
}, cancellationToken: cancellationToken);
await using var content = file.Content;
await content.CopyToAsync(destination, cancellationToken);
```

Dispose `Content` to release both the stream and its HTTP response. Pass cancellation
to subsequent stream reads/copies as well. Metadata reflects the selected endpoint:
the download endpoint may use `application/octet-stream`.

Explicit `download: false` still requests the view endpoint, but **partial responses
are rejected with `HttpRequestException`** rather than silently exposing a truncated
file. Large audio/video views can return an initial 4 MB range. Use `ProxyAsync` for
browser playback/range requests; `OpenReadAsync` does not assemble ranges.

Update and rebuild consuming applications to pick up the new default (`download: true`).
Previously compiled callers may still pass the old optional value (`false`). They will
now receive a clear error on partial content instead of silently reading an incomplete file.

## Typed Multipart Upload

Resolve the deployment from trusted application placement and use its existing
`IStorageClient`; no controller, proxy route, or fabricated `HttpContext` is needed:

```csharp
using Felina.Contracts;

var storage = storageClients.GetRequired(connectionName);
await using var stream = File.OpenRead(sourcePath);
var uploaded = await storage.UploadAsync(new UploadRequest
{
    Client = clientName,
    Module = moduleName,
    Workspace = workspaceName,
    FolderCuid = folderCuid,
    FileName = "report.pdf",
    ContentType = "application/pdf",
    Actor = actorId
}, stream, cancellationToken);

if (!uploaded.Status)
    return uploaded; // Preserve the failure in your application's feedback flow.

var versionUid = uploaded.Result.VersionUid;
var rootUid = uploaded.Result.RootUid;
```

This uploads **one file per call** using the configured Haley.Rest client, base path,
and service credentials. Scope and targets must be authorized by the consuming app.
`FileName` is a name, not a path; supply the actual MIME type in `ContentType`.

- With no UID, the Host applies its existing filename-based creation/versioning rules.
- Set `VersionUid` or `RootUid`, never both. `Replace=true` replaces the selected
  version (latest content for a root). `Replace=false` creates a new content version.
  The SDK supplies the matching multipart data key and RUID marker automatically.
- Set `Thumbnail=true` plus a target to add a thumbnail. Successful thumbnail uploads
  deliberately return null UIDs, not a new content reference.
- The result is Haley `IFeedback<UploadedFile>`: check `Status`, not merely HTTP success.
  Host-reported file failures preserve their message/key/code. HTTP errors throw
  `HttpRequestException`; invalid response shapes throw `InvalidDataException`;
  cancellation propagates. Internal storage paths and database IDs are not projected.
- The client prefers the Host's top-level `versionCuid` and `rootCuid`, and falls back
  to legacy `result.cuid` and `result.rootCuid` while older Hosts remain deployed.
  Consumer code should use `UploadedFile.VersionUid` and `RootUid`, not the Host wire
  response or its legacy nested `result` object.
- The caller owns the stream. It stays open even after failure/cancellation, starts at
  its current position, and can be non-seekable. Keep it open and do not share it with
  another reader during the upload. File bytes are not buffered in full.
- No automatic retry, ticket completion, batch upload, or TUS/chunk orchestration is
  performed. A lost response can mean a committed file: resolve before retrying.
  Configure the named connection timeout for the expected duration.

This method requires a package build containing the new API. Source changes alone do
not update existing installed packages. Endpoint proxy mapping remains available for
browser requests and resumable protocols; it is not required for this backend call.

## Internal Felina Service Auth

Felina Storage Host accepts service keys through:

- `X-Felina-Client`
- `X-Felina-Storage-Key`

Configure Felina Storage with SHA-256 hashes of service keys. Keep the admin password separate from these backend service keys.

Provision and rotate these credentials on the Felina Storage server. Never place a real
service key in source control or client-side browser code.
