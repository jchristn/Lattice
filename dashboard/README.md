# Lattice Dashboard

A React-based web dashboard for managing and interacting with Lattice document collections, schemas, and search functionality.

## Features

- **Collections Management** - Create, configure, and delete document collections
- **Document Operations** - View, create, search, and delete documents
- **Schema Management** - Configure schemas and schema elements
- **Index Management** - View index tables/entries and rebuild indexes
- **Full-text Search** - Search across documents with filtering
- **Light/Dark Theme** - Toggle between themes (persisted to localStorage)

## Getting Started (Development)

### Prerequisites

- Node.js 20 or later
- npm
- Lattice server running on port 5099 (for API calls)

### Installation

```bash
cd dashboard
npm install
```

### Running the Development Server

```bash
npm run dev
```

The dashboard will start at `http://localhost:3000`. The development server automatically proxies API requests (`/v1.0/*`) to `http://localhost:5099`.

### Development Features

- **Hot Module Replacement (HMR)** - Changes reflect instantly without full page reload
- **API Proxy** - Requests to `/v1.0` are forwarded to the backend server
- **ESLint** - Run `npm run lint` to check code quality

### Available Scripts

| Script | Description |
|--------|-------------|
| `npm run dev` | Start development server on port 3000 |
| `npm run build` | Build optimized production bundle to `dist/` |
| `npm run lint` | Run ESLint with strict rules |
| `npm run preview` | Preview production build locally |

## Production Deployment

### Building for Production

```bash
npm run build
```

This creates an optimized bundle in the `dist/` directory.

### Docker Deployment

The dashboard includes Docker configuration for production deployment.

#### Build the Docker Image

```bash
docker build -f docker/Dockerfile.dashboard -t lattice-dashboard .
```

#### Run with Docker Compose

From the project root:

```bash
docker compose up -d
```

This starts both the dashboard (port 3000) and the Lattice server (port 5099).

### Docker Configuration

**Dockerfile** (`docker/Dockerfile.dashboard`):
- Multi-stage build using Node 20 Alpine for building
- Nginx Alpine for serving static files
- Exposes port 80

**Nginx** (`docker/nginx.conf`):
- Gzip compression for static assets
- SPA routing (all routes fallback to `index.html`)
- API proxy: `/v1.0/*` routes to `http://server:5099`
- Swagger proxy: `/swagger/*` routes to `http://server:5099`
- Long-term caching (1 year) for static assets

### Manual Nginx Deployment

If deploying without Docker, configure nginx similarly:

```nginx
server {
    listen 80;
    root /path/to/dist;
    index index.html;

    # SPA routing
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy
    location /v1.0/ {
        proxy_pass http://localhost:5099;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## Project Structure

```
dashboard/
├── index.html              # HTML entry point
├── package.json            # Dependencies and scripts
├── vite.config.js          # Vite build configuration
├── public/                 # Static assets (logo, favicon)
└── src/
    ├── main.jsx            # React entry point
    ├── App.jsx             # Main component with routing
    ├── index.css           # Global styles and theming
    ├── context/
    │   └── AppContext.jsx  # Global state (server URL, theme)
    ├── utils/
    │   └── api.js          # API client
    ├── components/         # Reusable UI components
    │   ├── ActionMenu.jsx
    │   ├── CopyableId.jsx
    │   ├── KeyValueEditor.jsx
    │   ├── Modal.jsx
    │   ├── Sidebar.jsx
    │   ├── TagInput.jsx
    │   └── Topbar.jsx
    └── views/              # Page components
        ├── Login.jsx       # Server connection page
        ├── Dashboard.jsx   # Main layout
        ├── Collections.jsx
        ├── Documents.jsx
        ├── Search.jsx
        ├── Schemas.jsx
        ├── SchemaElements.jsx
        ├── Tables.jsx
        └── IndexEntries.jsx
```

## Configuration

### Runtime Configuration

The dashboard does not require environment variables. Configuration happens at runtime:

- **Server URL** - Enter the Lattice server URL on the login page (stored in localStorage)
- **Theme** - Toggle light/dark mode via the topbar (stored in localStorage)

### Build Configuration

The Vite configuration (`vite.config.js`) sets:
- Development server port: 3000
- API proxy target: `http://localhost:5099`

## Connecting to the Server

1. Start the Lattice server (default: `http://localhost:5099`)
2. Open the dashboard at `http://localhost:3000`
3. Enter the server URL on the login page
4. The dashboard validates the connection by fetching collections

## Technology Stack

- **React 19** - UI framework
- **React Router DOM 7** - Client-side routing
- **Vite 6** - Build tool and development server
- **Nginx** - Production web server (in Docker)
