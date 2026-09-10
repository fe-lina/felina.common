# Felina.Contracts

Shared public contracts used by Felina client libraries and consuming applications.

The current package contains Felina Storage file, folder, move, statistics, scope, and upload-ticket DTOs.
It deliberately excludes administration, registry mutation, shares, transfer monitoring,
throttling, persistence, SQL, and server implementation types.

The assembly namespace remains `Felina.Contracts` so package identity can evolve without
forcing namespace changes in consuming source code.
