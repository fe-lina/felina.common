# Felina Common

Public contracts and client libraries for integrating with Felina services.

## Projects

- `Felina.Contracts` contains stable public data-transfer objects shared by Felina client libraries.
- `Felina.Storage.Client` contains the supported Storage data-plane client and endpoint-proxy helpers.

This repository deliberately excludes server implementations, administration and control-plane
contracts, database schemas, migration scripts, deployment configuration, credentials, and
environment-specific values.

No open-source license is declared by this repository.

## Release operations

Package collection and publishing are owned by the sibling
`FelinaProject\NuGet Packaging` repository folder. This Common repository contains
only the public package source.
