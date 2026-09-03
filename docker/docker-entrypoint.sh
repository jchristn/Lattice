#!/bin/sh

# Generate runtime config from environment variables
cat > /usr/share/nginx/html/config.js <<EOF
window.__LATTICE_CONFIG__ = {
  serverUrl: "${LATTICE_SERVER_URL:-http://lattice-server:8000}",
  observability: {
    grafana: "${GRAFANA_URL:-http://localhost:3001}",
    prometheus: "${PROMETHEUS_URL:-http://localhost:9090}",
    tempo: "${TEMPO_URL:-http://localhost:3200}",
    loki: "${LOKI_URL:-http://localhost:3100}",
    otelCollector: "${OTEL_COLLECTOR_URL:-http://localhost:8889/metrics}"
  }
};
EOF

# Start nginx
exec nginx -g "daemon off;"
