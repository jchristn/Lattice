import './Observability.css'

const config = typeof window !== 'undefined' ? window.__LATTICE_CONFIG__ : undefined
const observabilityConfig = config?.observability || {}

const SERVICES = [
  {
    key: 'grafana',
    name: 'Grafana',
    purpose: 'Dashboards & visualization',
    credentials: 'admin / admin',
    url: observabilityConfig.grafana || 'http://localhost:3001',
  },
  {
    key: 'prometheus',
    name: 'Prometheus',
    purpose: 'Metrics store & queries',
    credentials: 'No login required',
    url: observabilityConfig.prometheus || 'http://localhost:9090',
  },
  {
    key: 'tempo',
    name: 'Tempo',
    purpose: 'Distributed traces (via Grafana Explore)',
    credentials: 'No login required',
    url: observabilityConfig.tempo || 'http://localhost:3200',
  },
  {
    key: 'loki',
    name: 'Loki',
    purpose: 'Logs (via Grafana Explore)',
    credentials: 'No login required',
    url: observabilityConfig.loki || 'http://localhost:3100',
  },
  {
    key: 'otelCollector',
    name: 'OpenTelemetry Collector',
    purpose: 'OTLP ingest / metrics export',
    credentials: 'No login required',
    url: observabilityConfig.otelCollector || 'http://localhost:8889/metrics',
  },
]

export default function Observability() {
  return (
    <div className="observability">
      <div className="page-header">
        <div>
          <h1 className="page-title">Observability</h1>
          <p className="page-subtitle">
            These are the telemetry tools the Lattice server exports to. Metrics, traces, and logs are
            pushed over OTLP to the OpenTelemetry Collector, which fans metrics to Prometheus, traces to
            Tempo, and logs to Loki. Grafana ties them together for dashboards and exploration. Each card
            opens the tool in a new browser tab.
          </p>
        </div>
      </div>

      <div className="observability-grid">
        {SERVICES.map((service) => (
          <a
            key={service.key}
            className="card observability-card"
            href={service.url}
            target="_blank"
            rel="noopener noreferrer"
          >
            <div className="observability-card-header">
              <h2>{service.name}</h2>
              <span className="observability-external" aria-hidden="true">↗</span>
            </div>
            <p className="observability-purpose">{service.purpose}</p>

            <div className="observability-meta">
              <span className="observability-meta-label">Default Credentials</span>
              <strong className="observability-meta-value">{service.credentials}</strong>
            </div>

            <div className="observability-meta">
              <span className="observability-meta-label">URL</span>
              <strong className="observability-meta-value observability-url">{service.url}</strong>
            </div>

            <span className="observability-open">Open in new tab ↗</span>
          </a>
        ))}
      </div>
    </div>
  )
}
