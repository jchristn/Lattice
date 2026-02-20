#!/bin/sh

# Generate runtime config from environment variables
cat > /usr/share/nginx/html/config.js <<EOF
window.__LATTICE_CONFIG__ = {
  serverUrl: "${LATTICE_SERVER_URL:-http://lattice-server:8000}"
};
EOF

# Start nginx
exec nginx -g "daemon off;"
