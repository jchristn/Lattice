<img src="https://raw.githubusercontent.com/jchristn/Lattice/main/assets/logo.png" alt="Lattice" width="128" height="128">

# Lattice

Lattice is a JSON document store with automatic schema detection, SQL-like querying, and flexible indexing. Ingest documents without defining their structure up front, query them with a familiar `WHERE field = 'value'` syntax, and let Lattice index and (optionally) validate them for you. It runs embedded against a single SQLite file or distributed across SQLite, SQL Server, PostgreSQL, or MySQL, and ships with a REST API, an MCP server, bearer-token auth with RBAC and multi-tenancy, a React dashboard, and built-in OpenTelemetry.

This page covers running Lattice from the published Docker images. The full source, SDKs, and reference documentation live at [github.com/jchristn/Lattice](https://github.com/jchristn/Lattice).

## Images

| Image | Purpose | Default Port |
|-------|---------|--------------|
| [`jchristn77/lattice`](https://hub.docker.com/r/jchristn77/lattice) | REST API + MCP server, storage, indexing, and search engine | 8000 |
| [`jchristn77/lattice-ui`](https://hub.docker.com/r/jchristn77/lattice-ui) | React management dashboard | 3000 |

Both images publish versioned tags (the current published tag is `v0.3.0`) alongside `latest`.

## What you can do with it

The core idea is that you should not have to design a schema before you can store data. Create a collection, `PUT` JSON documents into it, and Lattice flattens each document, detects its schema, and indexes its fields so they are immediately queryable — no migrations, no column definitions. Documents carry optional names, labels, and key-value tags for metadata, and you can layer schema constraints and selective indexing on at any time when you want more control.

Querying is SQL-like without being a full SQL engine. Filter with `=`, `!=`, `>`, `>=`, `<`, `<=`, `LIKE`, `IS NULL`, and `IS NOT NULL`, combine label and tag filters with a SQL expression, and page through results efficiently. Array fields are indexed element-by-element, so multi-valued data is first-class.

Everything is governed by authentication and RBAC. Callers authenticate with a session token (email and password, with the tenant inferred from the credentials) or with a credential access key presented directly as a bearer token. Roles evaluate deny-over-permit across resource types and operations, single-tier multi-tenancy isolates each tenant's collections and documents, and a security audit trail records authentication failures and authorization denials. Agents and LLM tools can drive the same surface through an in-process Model Context Protocol endpoint.

The dashboard puts a full UI over all of it — collections, documents, schemas, index tables, search, request history with a traffic chart, an API explorer, and admin consoles for tenants, users, credentials, roles, assignments, and the audit log.

<img src="https://raw.githubusercontent.com/jchristn/Lattice/main/assets/screenshot4.png?v=2" alt="Lattice dashboard — request history">

## Architecture

Lattice ships as two containers. The **server** (`jchristn77/lattice`) hosts the REST API, the MCP endpoint, and the storage/indexing/search engine in one process; it flattens and indexes documents on ingest, resolves queries against those indexes, and records request history for the dashboard. The **dashboard** (`jchristn77/lattice-ui`) is a static React app served by nginx that talks to the server's API.

State lives in a relational database. SQLite is the default in the Docker setup — a single file on a persisted volume — so a laptop and a server run the same way; SQL Server, PostgreSQL, and MySQL are also supported for shared, horizontally scaled deployments where multiple server instances sit in front of one database.

Observability is built in rather than bolted on. The server emits OpenTelemetry metrics and distributed traces across its critical paths — HTTP, ingestion, search, index rebuilds, and the database layer. The repository's Compose file ships a full stack (OpenTelemetry Collector, Prometheus, Tempo, Loki, and Grafana) with datasources and per-subsystem dashboards already provisioned, and you can point the same OTLP export at your own collector instead.

## Getting started

The complete stack — server, dashboard, and the observability services — is defined in `docker/compose.yaml` in the repository. Clone it and bring everything up:

```bash
git clone https://github.com/jchristn/Lattice.git
cd Lattice/docker
docker compose up -d
```

The dashboard comes up at `http://localhost:3000`, the API at `http://localhost:8000`, and Grafana at `http://localhost:3001`. On first run the server seeds a default tenant, administrator, and access key and prints them to its logs — save them, because the access key is shown there only once (though it can also be viewed later on the dashboard's Credentials page):

```bash
docker compose logs server | grep -iE "First run|Tenant id|Admin|Access key"
```

Sign in with `admin@lattice` / `password`. On first run there is a single tenant, so the tenant is inferred automatically at login.

If you only want the server with a single-file SQLite database, run the image on its own with a mounted configuration. Create `lattice.json`:

```json
{
  "Rest": { "Hostname": "*", "Port": 8000, "Ssl": false },
  "Lattice": { "Database": { "Type": "Sqlite", "Filename": "/app/data/lattice.db" } },
  "Auth": { "Enable": true }
}
```

Then start the container:

```bash
docker run -d --name lattice \
  -p 8000:8000 \
  -v "$(pwd)/lattice.json:/app/lattice.json:ro" \
  -v "$(pwd)/data:/app/data" \
  jchristn77/lattice:latest lattice.json
```

Watch the logs for the first-run credentials, then either drive the API directly or run the dashboard container against it.

## Ports

The Compose stack exposes the following. When you run containers individually, publish only what you need.

| Service | Port | Notes |
|---------|------|-------|
| Lattice server | 8000 | REST API and MCP endpoint |
| Lattice dashboard | 3000 | Management UI |
| Grafana | 3001 | Dashboards (host 3001; the dashboard owns host 3000) |
| Prometheus | 9090 | Metrics |
| Tempo | 3200 | Traces |
| Loki | 3100 | Logs |
| OpenTelemetry Collector | 4317 / 4318 | OTLP gRPC / HTTP ingest |

## Configuration

The server reads a JSON configuration file (`lattice.json`, mounted at `/app/lattice.json` and passed as the container's argument). If the file is absent it is created from defaults on first boot. The blocks that matter most are the REST binding and CORS, the database, authentication, the MCP server, request history, and telemetry.

The database block selects the provider. SQLite is the Docker default:

```json
{ "Lattice": { "Database": { "Type": "Sqlite", "Filename": "/app/data/lattice.db" } } }
```

Switching to PostgreSQL (or MySQL / SqlServer) is a matter of the connection fields:

```json
{
  "Lattice": {
    "Database": {
      "Type": "Postgres",
      "Hostname": "lattice-postgres",
      "Port": 5432,
      "DatabaseName": "lattice",
      "Username": "lattice",
      "Password": "lattice"
    }
  }
}
```

Authentication and the MCP endpoint are toggled by their own blocks (both enabled by default in the published images):

```json
{
  "Auth": { "Enable": true, "DefaultAdminEmail": "admin@lattice", "DefaultAdminPassword": "password" },
  "Mcp": { "Enable": true }
}
```

> **Change these for any shared deployment.** Set `DefaultAdminEmail`, `DefaultAdminPassword`, and `TokenSecret` in the `Auth` block before exposing the server — the defaults are for local use only.

Telemetry is configured through a `Telemetry` section (OTLP endpoint, in-process Prometheus scrape, sampling, Loki export). The Compose file wires it to the bundled collector out of the box.

## Authentication

Every route except health, the OpenAPI spec, and login requires a bearer token via the `Authorization: Bearer <value>` header (an `x-token` alias is also accepted). Two methods are supported:

1. **Session token** — `POST /v1.0/token` with an email and password returns a token; the tenant is inferred from the credentials, and the response asks you to choose only when the same email exists in more than one tenant.
2. **Access key** — a credential's access key (format `key_...`) is presented directly as the bearer value, ideal for machine-to-machine use.

## Learn more

- **Repository and full README:** [github.com/jchristn/Lattice](https://github.com/jchristn/Lattice)
- **REST API reference:** [REST_API.md](https://github.com/jchristn/Lattice/blob/main/REST_API.md)
- **MCP API reference:** [MCP_API.md](https://github.com/jchristn/Lattice/blob/main/MCP_API.md)
- **Telemetry guide:** [TELEMETRY.md](https://github.com/jchristn/Lattice/blob/main/TELEMETRY.md)
- **Changelog:** [CHANGELOG.md](https://github.com/jchristn/Lattice/blob/main/CHANGELOG.md)

Lattice is released under the MIT license.
