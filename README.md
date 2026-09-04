<p align="center">
  <img src="https://raw.githubusercontent.com/jchristn/Lattice/main/assets/logo.png" alt="Lattice Logo" width="192" height="192">
</p>

# Lattice

Lattice is a JSON document store with automatic schema detection, SQL-like querying, and flexible indexing. It supports multiple database backends including SQLite, SQL Server, PostgreSQL, and MySQL, enabling both embedded single-node deployments and horizontally scalable distributed architectures.

## Features

- **Automatic Schema Detection**: Ingest JSON documents without defining structure upfront
- **SQL-like Queries**: Familiar `WHERE field = 'value'` syntax
- **Multi-Database Support**: SQLite, SQL Server, PostgreSQL, MySQL
- **Horizontal Scaling**: Deploy multiple instances against a shared database backend
- **Automatic Indexing**: Fields indexed by default with selective override
- **Optional Schema Enforcement**: Add constraints at any time
- **REST API**: Built-in HTTP server for remote access
- **Authentication & RBAC**: Bearer-token auth, role-based access control, and single-tier multi-tenancy
- **MCP Server**: In-process Model Context Protocol endpoint for agents and LLM tools

## Screenshots

<details>
<summary><b>Click to expand — a tour of the dashboard</b></summary>

### Collections

<img src="https://raw.githubusercontent.com/jchristn/Lattice/main/assets/screenshot1.png" alt="Collections">

Browse and manage collections — each row shows its ID, name, description, and creation time, with per-column filters and quick actions.

### Editing a Collection

<img src="https://raw.githubusercontent.com/jchristn/Lattice/main/assets/screenshot2.png" alt="Editing a Collection">

Click any row to edit a collection's name and description; schema constraints and indexing are managed from their own dialogs.

### Documents

<img src="https://raw.githubusercontent.com/jchristn/Lattice/main/assets/screenshot3.png" alt="Documents">

Open a collection to page through its documents with per-column filters, inspect schema assignment, and add new documents with labels and tags.

### Request History

<img src="https://raw.githubusercontent.com/jchristn/Lattice/main/assets/screenshot4.png" alt="Request History">

Inspect captured request metadata, timings, headers, and bodies, with a traffic-summary chart of successes and failures over selectable time ranges.

### Searching Documents

<img src="https://raw.githubusercontent.com/jchristn/Lattice/main/assets/screenshot5.png" alt="Searching Documents">

Run label, tag, and SQL-style searches against a collection to validate query behavior and inspect the exact documents returned.

</details>

## Getting Started

The fastest way to run Lattice is with Docker Compose — it starts the server and the dashboard together:

```bash
docker compose up -d
```

- **Dashboard:** http://localhost:3000
- **API:** http://localhost:8000

Sign in with the default administrator seeded on first run — `admin@lattice` / `password`. See
[Default credentials](#default-credentials-first-run) to retrieve the generated tenant id and access key.
Data persists in the `lattice-data` and `lattice-documents` Docker volumes.

> Want to embed Lattice as a library or run the server from source instead? See [Installation](#installation)
> and [Quick Start](#quick-start) below.

## Installation

```bash
dotnet add package Lattice.Core
```

For the REST server:
```bash
dotnet add package Lattice.Server
```

## Quick Start

### Using LatticeClient (Embedded)

#### SQLite (Default)

```csharp
using Lattice.Core;

// Default SQLite configuration
LatticeSettings settings = new LatticeSettings();
settings.Database.Type = DatabaseTypeEnum.Sqlite;
settings.Database.Filename = "lattice.db";

using LatticeClient client = new LatticeClient(settings);

// Create a collection and add documents
await client.Collection.CreateAsync("users");
await client.Document.CreateAsync("users", new { name = "Alice", email = "alice@example.com" });
```

#### SQLite In-Memory

```csharp
using Lattice.Core;

LatticeSettings settings = new LatticeSettings
{
    InMemory = true
};
settings.Database.Type = DatabaseTypeEnum.Sqlite;
settings.Database.Filename = "lattice.db";

using LatticeClient client = new LatticeClient(settings);
```

#### SQL Server

```csharp
using Lattice.Core;
using Lattice.Core.Repositories.SqlServer;

// Option 1: Using individual parameters
SqlServerRepository repo = new SqlServerRepository(
    server: "localhost",
    database: "lattice",
    username: "sa",
    password: "YourPassword123!",
    trustServerCertificate: true
);
repo.InitializeRepository();

LatticeSettings settings = new LatticeSettings();
using LatticeClient client = new LatticeClient(repo, settings);

// Option 2: Using connection string
SqlServerRepository repo = new SqlServerRepository(
    "Server=localhost;Database=lattice;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
);
repo.InitializeRepository();

using LatticeClient client = new LatticeClient(repo, new LatticeSettings());
```

#### PostgreSQL

```csharp
using Lattice.Core;
using Lattice.Core.Repositories.Postgresql;

// Option 1: Using individual parameters
PostgresqlRepository repo = new PostgresqlRepository(
    host: "localhost",
    database: "lattice",
    username: "postgres",
    password: "YourPassword123!",
    port: 5432
);
repo.InitializeRepository();

LatticeSettings settings = new LatticeSettings();
using LatticeClient client = new LatticeClient(repo, settings);

// Option 2: Using connection string
PostgresqlRepository repo = new PostgresqlRepository(
    "Host=localhost;Port=5432;Database=lattice;Username=postgres;Password=YourPassword123!;Pooling=true;"
);
repo.InitializeRepository();

using LatticeClient client = new LatticeClient(repo, new LatticeSettings());
```

#### MySQL

```csharp
using Lattice.Core;
using Lattice.Core.Repositories.Mysql;

// Option 1: Using individual parameters
MysqlRepository repo = new MysqlRepository(
    server: "localhost",
    database: "lattice",
    username: "root",
    password: "YourPassword123!",
    port: 3306
);
repo.InitializeRepository();

LatticeSettings settings = new LatticeSettings();
using LatticeClient client = new LatticeClient(repo, settings);

// Option 2: Using connection string
MysqlRepository repo = new MysqlRepository(
    "Server=localhost;Port=3306;Database=lattice;User=root;Password=YourPassword123!;Pooling=true;"
);
repo.InitializeRepository();

using LatticeClient client = new LatticeClient(repo, new LatticeSettings());
```

### Using Lattice.Server (REST API)

Lattice.Server loads configuration from the `lattice.json` settings file and exposes a REST API.

#### Starting the Server

For most users, [Docker Compose](#getting-started) is the recommended way to run the server. To run it
directly from source during development:

```bash
# Use default settings file (lattice.json)
dotnet run --project src/Lattice.Server

# Use custom settings file
dotnet run --project src/Lattice.Server -- mysettings.json
```

#### Configuration File Examples

##### SQLite Configuration (lattice.json)

```json
{
  "Logging": {
    "ConsoleLogging": true,
    "MinimumSeverity": 1,
    "LogFilename": "lattice.log"
  },
  "Rest": {
    "Hostname": "localhost",
    "Port": 8000
  },
  "Lattice": {
    "InMemory": false,
    "DefaultDocumentsDirectory": "./documents",
    "EnableLogging": false,
    "Database": {
      "Type": "Sqlite",
      "Filename": "lattice.db"
    }
  }
}
```

##### SQL Server Configuration

```json
{
  "Logging": {
    "ConsoleLogging": true,
    "MinimumSeverity": 1,
    "LogFilename": "lattice.log"
  },
  "Rest": {
    "Hostname": "0.0.0.0",
    "Port": 8000
  },
  "Lattice": {
    "DefaultDocumentsDirectory": "./documents",
    "EnableLogging": false,
    "Database": {
      "Type": "SqlServer",
      "Hostname": "localhost",
      "Port": 1433,
      "DatabaseName": "lattice",
      "Username": "sa",
      "Password": "YourPassword123!"
    }
  }
}
```

##### PostgreSQL Configuration

```json
{
  "Logging": {
    "ConsoleLogging": true,
    "MinimumSeverity": 1,
    "LogFilename": "lattice.log"
  },
  "Rest": {
    "Hostname": "0.0.0.0",
    "Port": 8000
  },
  "Lattice": {
    "DefaultDocumentsDirectory": "./documents",
    "EnableLogging": false,
    "Database": {
      "Type": "Postgres",
      "Hostname": "localhost",
      "Port": 5432,
      "DatabaseName": "lattice",
      "Username": "postgres",
      "Password": "YourPassword123!"
    }
  }
}
```

##### MySQL Configuration

```json
{
  "Logging": {
    "ConsoleLogging": true,
    "MinimumSeverity": 1,
    "LogFilename": "lattice.log"
  },
  "Rest": {
    "Hostname": "0.0.0.0",
    "Port": 8000
  },
  "Lattice": {
    "DefaultDocumentsDirectory": "./documents",
    "EnableLogging": false,
    "Database": {
      "Type": "Mysql",
      "Hostname": "localhost",
      "Port": 3306,
      "DatabaseName": "lattice",
      "Username": "root",
      "Password": "YourPassword123!"
    }
  }
}
```

## Authentication & Multi-tenancy

When authentication is enabled (the default), every route except health, the OpenAPI spec, and login
requires a bearer token. There are two ways to authenticate, both presented with the
`Authorization: Bearer <value>` header:

1. **Session token** — log in with email and password to obtain a token. The tenant is inferred from the
   credentials (pass `tenantId` only to disambiguate when the email exists in more than one tenant):

   ```bash
   curl http://localhost:8000/v1.0/token \
     -H 'Content-Type: application/json' \
     -d '{"email":"admin@lattice","password":"password"}'
   # -> { "token": "...", "expiresUtc": "...", "tenantId": "...", "isAdmin": true, ... }
   # If the email maps to multiple tenants:
   # -> { "tenantSelectionRequired": true, "tenants": [ { "tenantId": "...", "tenantName": "..." }, ... ] }
   #    repeat with "tenantId" set to the chosen tenant.

   curl http://localhost:8000/v1.0/collections -H "Authorization: Bearer <token>"
   ```

2. **Access key** — present a credential's access key (`key_...`) directly as the bearer value:

   ```bash
   curl http://localhost:8000/v1.0/collections -H "Authorization: Bearer key_..."
   ```

Access is governed by role-based access control (deny-over-permit) with built-in roles. Collections and
their documents are isolated by tenant — a principal sees and acts on only its own tenant's collections
(and, transitively, their documents); a system administrator sees all tenants. Authentication and MCP are
configured under the `Auth` and `Mcp` blocks in `lattice.json`. See [`REST_API.md`](REST_API.md) for the
full auth surface and [`MCP_API.md`](MCP_API.md) for the Model Context Protocol endpoint.

### Default credentials (first run)

On its first run against an empty database, the server seeds a default tenant, an administrator, and an
access key, and prints them to the console **once**. The email and password are fixed defaults; the tenant
id and access key are generated per install.

| Credential | Value |
|---|---|
| Admin email | `admin@lattice` (configurable) |
| Admin password | `password` (configurable) |
| Tenant id | generated (`ten_...`) — shown in the log |
| Access key | generated (`key_...`) — shown in the log; also viewable later on the dashboard's Credentials page |

The email and password default to the `Auth` block in `lattice.json` (`DefaultAdminEmail`,
`DefaultAdminPassword`); the tenant id and access key are printed on the run that creates them. The raw
access key is also persisted, so you can view (and edit) it afterward on the dashboard's **Credentials**
page or by reading the credential via the API. Retrieve the first-run values from the server log — for the
Docker deployment:

```bash
docker logs lattice-server | grep -iE "First run|Tenant id|Admin|Access key"
```

```
 First run: default credentials (store these now, shown only once)
   Tenant id:    ten_...
   Admin email:  admin@lattice
   Admin passwd: password
   Access key:   key_...
```

To sign in:

- **Access key (quickest):** paste the `key_...` value into the dashboard's "Access key" login tab, or
  send it as `Authorization: Bearer key_...`. No tenant id required.
- **Email + password:** use `admin@lattice` / `password`. On first run there is only one tenant, so the
  tenant is inferred automatically — you only need to supply a tenant id once the same email exists in more
  than one tenant.

> **Change these for any shared deployment.** Set `DefaultAdminEmail`, `DefaultAdminPassword`, and
> `TokenSecret` in `lattice.json` (for Docker, `docker/server/lattice.json`) before exposing the server —
> the defaults are for local use only.

## Horizontal Scaling

With SQL Server, PostgreSQL, or MySQL backends, multiple Lattice instances can share a common database, enabling horizontal scaling:

```
                    ┌─────────────────┐
                    │  Load Balancer  │
                    └────────┬────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
         ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ Lattice.Server  │ │ Lattice.Server  │ │ Lattice.Server  │
│   Instance 1    │ │   Instance 2    │ │   Instance 3    │
└────────┬────────┘ └────────┬────────┘ └────────┬────────┘
         │                   │                   │
         └───────────────────┼───────────────────┘
                             │
                             ▼
                    ┌──────────────────┐
                    │  Shared Database │
                    │  (PostgreSQL /   │
                    │   SQL Server /   │
                    │   MySQL)         │
                    └──────────────────┘
```

Each instance connects to the same database, allowing:
- **Load distribution** across multiple server instances
- **High availability** through redundant instances
- **Independent scaling** of application and database tiers

## Core Operations

### Collections

```csharp
// Create a collection
await client.Collection.CreateAsync("products");

// List collections (returns an EnumerationResult; items are in .Objects)
EnumerationResult<Collection> collections = await client.Collection.ReadAllAsync();
foreach (Collection collection in collections.Objects) { /* ... */ }

// Delete a collection
await client.Collection.DeleteAsync("products");
```

### Documents

```csharp
// Ingest a document
Collection col = await client.Collection.Create("products");
Document doc = await client.Document.Ingest(
    col.Id,
    @"{""name"":""Widget"",""price"":29.99}",
    name: "widget.json",
    labels: new List<string> { "hardware" },
    tags: new Dictionary<string, string> { ["category"] = "tools" });

// Batch ingest multiple documents
List<Document> docs = await client.Document.IngestBatch(
    col.Id,
    new List<BatchDocument>
    {
        new BatchDocument(@"{""name"":""Widget A"",""price"":19.99}", name: "a.json"),
        new BatchDocument(@"{""name"":""Widget B"",""price"":29.99}", name: "b.json"),
        new BatchDocument(@"{""name"":""Widget C"",""price"":39.99}", name: "c.json")
    });

// Read a document
Document doc = await client.Document.ReadById(documentId, includeContent: true);

// Check existence
bool exists = await client.Document.Exists(documentId);

// Delete a document
await client.Document.Delete(documentId);
```

### Searching

```csharp
// Structured filter query
SearchResult result = await client.Search.Search(new SearchQuery
{
    CollectionId = col.Id,
    Filters = new List<SearchFilter>
    {
        new SearchFilter("name", SearchConditionEnum.Equals, "Widget")
    }
});

// SQL-like query
SearchResult result = await client.Search.SearchBySql(
    col.Id,
    "SELECT * FROM documents WHERE price > 20 AND price < 50 ORDER BY price ASC LIMIT 100");

// Paginated query
SearchResult result = await client.Search.Search(new SearchQuery
{
    CollectionId = col.Id,
    MaxResults = 100,
    Skip = 0,
    Ordering = EnumerationOrderEnum.CreatedDescending
});
```

### Supported Query Operators

- `=` Equal
- `!=` Not equal
- `>` Greater than
- `>=` Greater than or equal
- `<` Less than
- `<=` Less than or equal
- `LIKE` Pattern matching (with `%` wildcard)
- `IS NULL` Null check
- `IS NOT NULL` Not null check

## Project Structure

```
src/
├── Lattice.Core/           # Core library (NuGet: Lattice)
│   ├── Client/             # Client-facing API
│   ├── Models/             # Data models
│   ├── Repositories/       # Database implementations
│   │   ├── Sqlite/
│   │   ├── SqlServer/
│   │   ├── Postgresql/
│   │   └── Mysql/
│   ├── Schema/             # Schema detection
│   └── Search/             # Query parsing
├── Lattice.Server/         # REST API server
├── Lattice.LoadGenerator/  # Synthetic data seeder for demos/screenshots
├── Test.Automated/         # Integration tests
└── Test.Throughput/        # Performance tests
sdk/
├── sdk-csharp/             # C# REST SDK (NuGet: Lattice.Sdk)
├── sdk-js/                 # JavaScript/TypeScript SDK (npm: lattice-sdk)
└── sdk-python/             # Python SDK (pip: lattice-sdk)
```

## Building and Testing

```bash
# Build the solution
dotnet build

# Run automated tests
dotnet run --project src/Test.Automated

# Run throughput tests
dotnet run --project src/Test.Throughput
```

## Docker

Docker Compose is the default way to run Lattice (see [Getting Started](#getting-started)); it runs both
the server and dashboard together:

```bash
# Start both server and dashboard
docker compose up -d

# Access the dashboard at http://localhost:3000
# Access the API directly at http://localhost:8000
```

This starts:
- **Lattice Server** on port 8000 with SQLite storage
- **Lattice Dashboard** on port 3000 (proxies API requests to the server)

Data is persisted in Docker volumes (`lattice-data` and `lattice-documents`).

For detailed Docker configuration including external database backends, environment variables, and production deployment, see [DOCKER.md](DOCKER.md).

## Load Generator (Demo Data)

`Lattice.LoadGenerator` seeds a database with realistic synthetic activity — collections, documents,
backdated request history, audit entries, and identity/RBAC data — so the dashboard and observability
stack render like a real, in-use system for demos and screenshots. It writes directly to the database and
backdates activity across a time window (using a diurnal, bursty distribution), which a series of live API
calls could not reproduce.

To populate a running Docker deployment, point it at the server's SQLite database and refresh the
dashboard:

```bash
dotnet run --project src/Lattice.LoadGenerator -- \
  --backend sqlite --sqlite-file docker/server/data/lattice.db --density medium --days 14 --wipe
```

Everything is configurable via CLI arguments — backend and connection, target tenant, information density,
time range, and which categories of activity to generate (all by default). A few examples:

```bash
# A dense, month-long history
dotnet run --project src/Lattice.LoadGenerator -- --backend sqlite --sqlite-file lattice.db --density high --days 30

# Only collections, documents, and request history
dotnet run --project src/Lattice.LoadGenerator -- --backend sqlite --sqlite-file lattice.db --operations collections,documents,requests

# A PostgreSQL backend
dotnet run --project src/Lattice.LoadGenerator -- --backend postgresql --host localhost --port 5432 \
  --database lattice --username lattice --password lattice

# Remove previously generated synthetic entities
dotnet run --project src/Lattice.LoadGenerator -- --backend sqlite --sqlite-file lattice.db --wipe-only
```

Run with `--help` for the full option list. Synthetic entities are marked (collections labelled
`synthetic`, users under `@loadgen.synthetic`, roles prefixed `LG-`) so `--wipe` can clean them up. Set
`--server-url` and `--access-key` with `--live-requests` to also fire a burst of live requests so
telemetry and Grafana light up.

## License

MIT License. See the [LICENSE](LICENSE) file for details.

## Attribution

Special thanks to <a href="https://www.flaticon.com/free-icons/particles" title="particles icons">Particles icons created by Aranagraphics - Flaticon</a> for the logo.
