# Lattice Telemetry

Lattice is fully instrumented for observability. Every HTTP request, every core-engine service call,
every critical workflow (ingestion, search, index rebuild), the database layer, and the background
request-history worker emit **metrics** and **traces** through the .NET base class library
(`System.Diagnostics.Metrics.Meter` and `System.Diagnostics.ActivitySource`). Structured **logs** are
emitted through `Microsoft.Extensions.Logging`. All three signals are exported over OTLP and can be wired
into any OpenTelemetry-compatible backend (Prometheus, Tempo, Loki, Grafana, Datadog, Honeycomb, Grafana
Cloud, etc.).

This document describes what is available, how to turn it on and configure it, and how a devops team
connects it to a broader observability stack.

---

## 1. Design in one paragraph

Instrumentation **rides the BCL**. The engine (`Lattice.Core`) and the server (`Lattice.Server`) create
static `Meter` and `ActivitySource` instances named `Lattice.Core` and `Lattice.Server` and emit through
them directly — with **no dependency on any telemetry SDK**. When nothing is listening, every
measurement is a near-zero-cost no-op. At startup the server starts a single **telemetry host** (built on
the [Radiant](https://www.nuget.org/packages/Radiant) SDK, a thin OpenTelemetry wrapper) that subscribes
to those two names, builds the OpenTelemetry `MeterProvider` / `TracerProvider` / logging pipeline, and
exports. Because emit and host meet only at the string names, you can point Lattice at your own collector
without touching a line of application code.

```
Lattice.Core  ─┐                          ┌─ Prometheus (metrics)
Lattice.Server ┼─► Radiant host ─► OTLP ─►│─ Tempo      (traces)
 (BCL emit)    ─┘   (in Lattice.Server)    └─ Loki       (logs)
                          │                        │
                          └─ in-process            └─ Grafana (dashboards + correlation)
                             Prometheus /metrics
```

---

## 2. What is instrumented

### 2.1 Metrics

Resource attributes stamped on everything: `service.name` (default `lattice-server`) and
`service.instance.id` (a stable per-process GUID). In Prometheus these become the labels `service_name`
and `service_instance_id`. Dotted OTel instrument names convert to Prometheus names by replacing `.` with
`_`, appending the unit, adding `_total` for counters and `_bucket`/`_sum`/`_count` for histograms.

**HTTP** (meter `Lattice.Server`) — 100% of inbound requests, captured in Watson `PreRouting`/`PostRouting`
so health checks, Swagger, and 404s are included:

| Instrument | Type | Unit | Key labels |
| --- | --- | --- | --- |
| `http.server.request.count` | Counter | `{request}` | `http.request.method`, `http.response.status_code`, `lattice.request_type` |
| `http.server.request.duration` | Histogram | `s` | `http.request.method`, `http.response.status_code`, `lattice.request_type` |
| `http.server.active_requests` | UpDownCounter | `{request}` | `http.request.method` |
| `http.server.request.body.size` | Histogram | `By` | `lattice.request_type`, `http.request.method` |
| `http.server.response.body.size` | Histogram | `By` | `lattice.request_type`, `http.response.status_code` |

`lattice.request_type` is a low-cardinality class: `health`, `collection`, `document`, `search`, `schema`,
`table`, `requesthistory`, `swagger`, `other`.

**Services, workflows & database** (meter `Lattice.Core`):

| Instrument | Type | Unit | Key labels |
| --- | --- | --- | --- |
| `lattice.operations` | Counter | `{operation}` | `operation`, `outcome`, `db.system` |
| `lattice.operation.duration` | Histogram | `s` | `operation`, `outcome`, `db.system` |
| `lattice.documents.ingested` | Counter | `{document}` | `collection`, `mode` (`single`\|`batch`) |
| `lattice.ingest.batch.size` | Histogram | `{document}` | `collection` |
| `lattice.search.results` | Histogram | `{document}` | `collection` |
| `lattice.schemas.created` | Counter | `{schema}` | `collection` |
| `lattice.index.tables.created` | Counter | `{table}` | — |
| `lattice.index.rebuilds` | Counter | `{operation}` | `collection`, `outcome` |
| `lattice.lock.contention` | Counter | `{event}` | `collection` |

`operation` covers every public engine call, e.g. `collection.create`, `document.ingest`,
`document.ingest_batch`, `search.query`, `search.sql`, `index.rebuild`, `schema.read`, `index.get_mappings`.
`outcome` is `ok` or `error`. `db.system` is `sqlite`, `mysql`, `postgresql`, or `sqlserver` — the same
histogram therefore doubles as the database-layer timing (measured at the repository-call boundary).

**Background worker** (meter `Lattice.Server`):

| Instrument | Type | Labels |
| --- | --- | --- |
| `lattice.requesthistory.recorded` | Counter | `outcome` |
| `lattice.requesthistory.prune.runs` | Counter | `outcome` |
| `lattice.requesthistory.pruned` | Counter | — |

**Process & runtime** (always on when metrics are enabled): `process_memory_usage_bytes`,
`process_uptime_seconds`, `process_thread_count`, plus the full `process_runtime_dotnet_*` set (GC
collections, heap size, allocations, thread-pool, JIT) from `OpenTelemetry.Instrumentation.Runtime`.

### 2.2 Traces

Two activity sources: `Lattice.Server` and `Lattice.Core`.

- **HTTP server span** (`Lattice.Server`, kind `Server`) — one per functional API request, named
  `{METHOD} {request_type}`, tagged with method, path, request type, collection/document id, and final
  status code. On failure the span is marked `Error`.
- **Operation spans** (`Lattice.Core`, kind `Internal`) — one per engine operation, named after the
  operation (e.g. `document.ingest`). Tagged with `operation`, `db.system`, `lattice.collection`, and
  `outcome`. Milestone events are attached: `schema.created`, `index.table.created`, `lock.contended`,
  and an `exception` event with type/message/stacktrace on error.

Core spans nest under the HTTP server span (they run in the same async context), so a single trace shows
the request → engine operation → milestones end to end. Sampling is head-based and parent-based (a sampled
parent keeps its children); the ratio is configurable.

### 2.3 Logs

When the logs pillar is enabled, records written through the host logger are exported over OTLP (to the
collector) and, optionally, directly to Loki. Log records are stamped with the active `trace_id` /
`span_id` for trace↔log correlation. The existing `SyslogLogging` console/file logging is unchanged and
remains the primary local log sink; OTLP logs add lifecycle and error events to your central stack.

---

## 3. Enabling and configuring

Telemetry is controlled by the `Telemetry` section of the server config file (`lattice.json`). It is
**on by default** and, with no collector present, is a harmless no-op that retries export quietly. Full
schema with defaults:

```json
{
  "Telemetry": {
    "Enable": true,
    "ServiceName": "lattice-server",
    "ServiceInstanceId": null,

    "Otlp": {
      "Enable": true,
      "Endpoint": "http://localhost:4317",
      "Protocol": "Grpc",
      "TimeoutMs": 10000
    },

    "Metrics": {
      "Enable": true,
      "ExportIntervalMs": 15000,
      "IncludeRuntime": true,
      "IncludeProcess": true
    },

    "Traces": {
      "Enable": true,
      "SamplingRatio": 1.0
    },

    "Logs": {
      "Enable": true,
      "MinimumSeverity": 2
    },

    "Prometheus": {
      "Enable": true,
      "Hostname": "localhost",
      "Port": 9464,
      "Path": "/metrics"
    },

    "Loki": {
      "Enable": false,
      "Endpoint": "http://localhost:3100/otlp",
      "TenantId": null
    }
  }
}
```

Field notes:

- **`Enable`** — master switch. `false` starts no host; instrumentation stays inert (near-zero overhead).
- **`Otlp.Endpoint` / `Protocol`** — where to push. `Grpc` → port 4317; `HttpProtobuf` → port 4318. Point
  this at your own OpenTelemetry Collector (or a vendor OTLP endpoint) to leave the bundled stack behind.
- **`Otlp` headers** — for a hosted/authenticated collector, add headers (e.g. an API key) — see §6.
- **`Metrics.ExportIntervalMs`** — OTLP push cadence (1000–300000 ms). The in-process scrape is pull-based
  and ignores this.
- **`Traces.SamplingRatio`** — `1.0` samples everything; lower it (e.g. `0.1`) in high-throughput
  production.
- **`Logs.MinimumSeverity`** — 0–7 syslog-style (0 Trace … 7 None); default 2 keeps Information and above.
- **`Prometheus`** — an in-process scrape endpoint (`http://<host>:9464/metrics`) so Prometheus can scrape
  the app directly, no collector required. Disable it when running behind the collector (as the container
  config does) to avoid binding a port you don't scrape.
- **`Loki`** — direct OTLP-HTTP log push to Loki, bypassing the collector. Leave off when the collector
  already forwards logs to Loki.

The containerized server (`docker/server/lattice.json`) ships with `Otlp.Endpoint` set to
`http://otel-collector:4317` and the in-process Prometheus endpoint disabled, since it exports through the
collector.

---

## 4. The bundled observability stack

A complete local stack lives in `docker/compose.yaml` and `docker/observability/`. Bring it up with:

```bash
cd docker
docker compose up -d
```

Services and host ports (chosen to avoid conflicts with the API on `8000` and the dashboard on `3000`):

| Service | URL | Default credentials | Purpose |
| --- | --- | --- | --- |
| **Grafana** | http://localhost:3001 | `admin` / `admin` | Dashboards, Explore, correlation |
| **Prometheus** | http://localhost:9090 | none | Metrics store & queries |
| **Tempo** | http://localhost:3200 | none | Trace store (query via Grafana) |
| **Loki** | http://localhost:3100 | none | Log store (query via Grafana) |
| **OTel Collector** | http://localhost:8889/metrics | none | OTLP ingest (4317 gRPC / 4318 HTTP), metrics export |

These same links (name, credentials, URL) are surfaced inside the product dashboard on the
**Observability** page for convenience; the URLs are overridable via the dashboard's runtime config
(`GRAFANA_URL`, `PROMETHEUS_URL`, `TEMPO_URL`, `LOKI_URL`, `OTEL_COLLECTOR_URL` env vars on the
`dashboard` container).

The collector fans OTLP out to Prometheus (metrics, pulled from the collector's `:8889`), Tempo (traces),
and Loki (logs). Grafana is provisioned with all three datasources and full bidirectional correlation:
metric exemplar → trace (Prometheus→Tempo), trace → logs (Tempo→Loki), and log line → trace (Loki→Tempo).

### Grafana dashboards

Provisioned into a single top-level Grafana folder named **Lattice** (flat — no subfolders; dashboards are
split by domain):

- **Lattice - Overview** — golden-signal health: request/error rate, p95 latency, in-flight, ingest rate,
  memory, uptime.
- **Lattice - HTTP** — request rate by status/type, latency quantiles, active requests, body sizes.
- **Lattice - Services** — operation rate, error rate, and p95 duration by `operation`.
- **Lattice - Ingestion & Workflows** — documents ingested, batch sizes, schemas & index tables created,
  index rebuilds, lock contention, ingest latency.
- **Lattice - Database** — operation rate/latency/errors by `db.system`.
- **Lattice - Runtime & Process** — memory, threads, uptime, and .NET GC/heap/thread-pool.
- **Lattice - Logs (Loki)** — live logs, log-rate, and errors-only for the service.

---

## 5. Connecting to your own stack

You do **not** need the bundled stack. To integrate with an existing observability platform:

**Option A — point at your collector (recommended).** Set `Telemetry.Otlp.Endpoint` to your OpenTelemetry
Collector or vendor OTLP endpoint and keep pushing all three signals:

```json
"Otlp": { "Enable": true, "Endpoint": "http://otel-collector.mycorp.internal:4317", "Protocol": "Grpc" }
```

Your collector then routes to wherever you already send telemetry (Prometheus/Mimir, Tempo/Jaeger, Loki,
Datadog, Honeycomb, Grafana Cloud, …). This is the least-coupled path — Lattice only speaks OTLP.

**Option B — scrape the app directly.** Leave `Prometheus.Enable = true` and add a scrape job to your
Prometheus:

```yaml
scrape_configs:
  - job_name: lattice
    static_configs:
      - targets: ["lattice-host:9464"]
```

Bind the scrape endpoint on all interfaces for remote scraping by setting `Prometheus.Hostname` to `+` or
`*` (on Windows this needs an HTTP namespace reservation). Metrics-only; traces and logs still need OTLP.

**Option C — direct Loki.** Set `Loki.Enable = true` and `Loki.Endpoint` to your Loki OTLP endpoint to push
logs to Loki without a collector in the path.

The vendor-neutral metric/trace/log semantics (OTel semantic-convention names, `service.name` /
`service.instance.id` resource attributes) mean dashboards and alerts you already have for OTel workloads
apply with minimal changes.

---

## 6. Notes for devops / SRE

- **Cardinality is bounded by design.** Metric labels are deliberately low-cardinality (`operation`,
  `outcome`, `db.system`, coarse `request_type`, method, status class). Document/collection ids are **not**
  used as metric labels (they appear only as span tags), so metric series counts stay flat as data grows.
- **Sampling.** Traces default to 100% sampling (`SamplingRatio: 1.0`) — fine for dev and moderate load.
  For high-throughput production, lower it (e.g. `0.05`–`0.2`); the parent-based sampler keeps each trace
  internally consistent. Metrics and logs are unaffected by trace sampling.
- **Overhead.** With no host (or `Enable: false`), instrumentation is a no-op. With a host, cost is a
  counter add + histogram record + a span per operation; the periodic OTLP exporter batches on a
  background thread (`ExportIntervalMs`).
- **Authenticated OTLP.** For hosted collectors requiring auth, extend the config with OTLP headers (e.g.
  `Authorization` / API-key). If your deployment needs headers exposed in `lattice.json`, add them to the
  `Otlp` section; they are passed through to the exporter as OTLP `key=value` headers. Keep secrets in
  environment-injected config, not in source control.
- **Resource identification.** Set `Telemetry.ServiceName` per deployment/environment (e.g.
  `lattice-server-prod`) and optionally a stable `ServiceInstanceId` per replica so `service_instance_id`
  is meaningful across restarts. Add environment/region as collector-side resource attributes if desired.
- **Persistence.** The bundled Prometheus/Tempo/Loki use named Docker volumes (`prometheus-data`,
  `tempo-data`, `loki-data`, `grafana-data`). For real retention, use your platform's managed stores.
- **Ports.** In-process scrape `9464`; collector OTLP `4317`/`4318`. Ensure these are reachable from your
  Prometheus/collector and firewalled appropriately.
- **Security.** The bundled stack is unauthenticated local-dev tooling (Grafana `admin/admin`, open
  Prometheus/Tempo/Loki, `tls.insecure` on collector exporters). Do **not** expose it as-is; put it behind
  your own auth/network controls or replace it with your managed backends for anything shared.
- **Health vs. telemetry.** The existing `GET /` and `GET /v1.0/health` endpoints remain the liveness
  signal; telemetry is for trends, latency, and diagnostics, not liveness gating.

---

## 7. Extending the instrumentation

To add your own measurements to core code, use the emit-side helper (no SDK dependency):

```csharp
using Lattice.Core.Telemetry;

using (OperationScope op = LatticeTelemetry.StartOperation("my.operation", collectionId))
{
    try
    {
        // ... work ...
        // op records lattice.operations + lattice.operation.duration and closes the span on dispose
    }
    catch (Exception e)
    {
        op.Fail(e);   // marks outcome=error, records the exception on the span
        throw;
    }
}
```

For custom counters/histograms, add instruments to the `Lattice.Core` meter in
`Lattice.Core/Telemetry/LatticeTelemetry.cs` (or the `Lattice.Server` meter in
`Lattice.Server/Telemetry/ServerTelemetry.cs`). Any host already subscribed to those meter names picks
them up automatically — no host changes required. Third-party libraries that expose their own `Meter` /
`ActivitySource` can be collected too by adding their names to `Telemetry.Sources` on the host.

---

## 8. Quick start

```bash
# 1. Start the observability stack (collector, Prometheus, Tempo, Loki, Grafana)
cd docker && docker compose up -d

# 2. Run Lattice pointing OTLP at the collector (the containerized server does this already;
#    for a local dev run, the defaults in lattice.json point at localhost:4317 / :9464).

# 3. Generate traffic (ingest, search, etc.), then open Grafana
open http://localhost:3001      # admin / admin  →  Dashboards → Lattice
```
