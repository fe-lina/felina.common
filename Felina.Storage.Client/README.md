# Felina.Storage.Client

Reusable app-facing proxy helpers for services that keep Felina Storage internal.

Install only the client package in normal consuming APIs:

```xml
<PackageReference Include="Felina.Storage.Client" Version="0.0.1" />
```

`Felina.Contracts` is brought in transitively. Reference that smaller
package directly only when a project needs the data-plane DTOs but not the HTTP client
or endpoint-mapping helpers. The internal `Felina.Storage.Contracts` project is not a
public NuGet package.

`/api/va/admin/*` is deliberately not part of this package. `IStorageClient` exposes
only the storage data plane, and its URL/proxy methods reject `admin` targets. Registry
mutation, runtime activation, startup persistence, and stats maintenance belong only to
the private Admin Host control-plane client.

The consuming API owns business authentication and authorization. This package only:

- forwards requests to internal Felina Storage
- adds the backend service key headers
- maps reusable proxy endpoints when useful
- creates and validates signed view tokens

## Internal Client

```csharp
using Felina.Client;

builder.Services.AddStorageClient(builder.Configuration);
```

```json
{
  "Felina": {
    "Storage": {
      "Client": {
        "Url": "base=http://felina-storage:5000/;suffix=api/va/;",
        "TimeoutSeconds": 300,
        "Credential": {
          "Required": true,
          "Id": "document-service-api",
          "Secret": "<random-service-key>",
          "ClientHeader": "X-Felina-Client",
          "KeyHeader": "X-Felina-Storage-Key"
        }
      }
    }
  }
}
```

Use named clients when one application may have more than one Felina endpoint or
credential. `Clients` is retained as an explicit collection boundary; shared transport
values can be placed under `Felina:Defaults`, while each client can override them.

```csharp
builder.Services.AddStorageClients(builder.Configuration);

var storage = factory.GetRequiredClient("Documents");
```

```json
{
  "Felina": {
    "Defaults": {
      "TimeoutSeconds": 300,
      "Credential": {
        "ClientHeader": "X-Felina-Client",
        "KeyHeader": "X-Felina-Storage-Key"
      }
    },
    "Clients": {
      "Documents": {
        "Url": "base=http://felina-storage:5000/;suffix=api/va/;",
        "Credential": {
          "Required": true,
          "Id": "document-service-api",
          "Secret": "<random-service-key>"
        }
      }
    }
  }
}
```

The client alias (`Documents`) is application configuration. It is not Felina's `c`
scope. Products should resolve their own target aliases to fixed `c/m/w` values and must
not accept those raw values from a browser.

## Endpoint Mapping

Map routes one by one when each route needs its own policy:

```csharp
app.MapStorageProxy("/api/docs/file", "file", HttpMethods.Post, options =>
{
    options.ClientName = "Documents";
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
`StorageMaxSizeMegabytesHeaderName` only when proxying to an older storage API that still
expects a different header.

Map the standard storage surface when one group-level policy is enough:

```csharp
var docs = app.MapStorageProxyDefaults("/api/docs");
docs.Group.RequireAuthorization("General");
```

The default mapping includes file and folder operations, the native chunk lifecycle,
and TUS discovery/create/resume/append/termination routes. It never maps Storage API
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

## Internal Felina Service Auth

Felina Storage API accepts service keys through:

- `X-Felina-Client`
- `X-Felina-Storage-Key`

Configure Felina Storage with SHA-256 hashes of service keys. Keep the admin password separate from these backend service keys.

Provision and rotate these credentials on the Felina Storage server. Never place a real
service key in source control or client-side browser code.
