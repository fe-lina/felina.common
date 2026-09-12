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

## Internal Felina Service Auth

Felina Storage Host accepts service keys through:

- `X-Felina-Client`
- `X-Felina-Storage-Key`

Configure Felina Storage with SHA-256 hashes of service keys. Keep the admin password separate from these backend service keys.

Provision and rotate these credentials on the Felina Storage server. Never place a real
service key in source control or client-side browser code.
