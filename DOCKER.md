# Docker Deployment Guide

This guide covers deploying Lattice using Docker, including configuration options, external database backends, and production considerations.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) 20.10 or later
- [Docker Compose](https://docs.docker.com/compose/install/) v2.0 or later

## Quick Start

Start the complete Lattice stack with a single command:

```bash
docker-compose up -d
```

This launches:
- **Lattice Server** at http://localhost:5099 - REST API with SQLite backend
- **Lattice Dashboard** at http://localhost:3000 - Web UI for managing collections and documents

To stop the services:

```bash
docker-compose down
```

To stop and remove all data:

```bash
docker-compose down -v
```

## Services Overview

### Lattice Server

The server provides the REST API and handles all data operations.

| Setting | Default |
|---------|---------|
| Port | 8000 |
| Database | SQLite |
| Data Volume | `lattice-data` |
| Documents Volume | `lattice-documents` |

### Lattice Dashboard

The dashboard provides a web interface for managing Lattice. It proxies API requests to the server automatically.

| Setting | Default |
|---------|---------|
| Port | 3000 |
| Backend | http://server:8000 |

## Environment Variables

The Lattice Server can be configured using environment variables. Add them to the `environment` section in `docker-compose.yml`:

### Database Configuration

```yaml
environment:
  # SQLite (default)
  - Lattice__Database__Type=Sqlite
  - Lattice__Database__Filename=/app/data/lattice.db

  # PostgreSQL
  - Lattice__Database__Type=Postgres
  - Lattice__Database__Hostname=postgres
  - Lattice__Database__Port=5432
  - Lattice__Database__DatabaseName=lattice
  - Lattice__Database__Username=postgres
  - Lattice__Database__Password=YourPassword123!

  # SQL Server
  - Lattice__Database__Type=SqlServer
  - Lattice__Database__Hostname=sqlserver
  - Lattice__Database__Port=1433
  - Lattice__Database__DatabaseName=lattice
  - Lattice__Database__Username=sa
  - Lattice__Database__Password=YourPassword123!

  # MySQL
  - Lattice__Database__Type=Mysql
  - Lattice__Database__Hostname=mysql
  - Lattice__Database__Port=3306
  - Lattice__Database__DatabaseName=lattice
  - Lattice__Database__Username=root
  - Lattice__Database__Password=YourPassword123!
```

### General Settings

```yaml
environment:
  - Lattice__InMemory=false
  - Lattice__DefaultDocumentsDirectory=/app/documents
  - Lattice__EnableLogging=false
```

## Using External Databases

For production deployments or horizontal scaling, you can use PostgreSQL, SQL Server, or MySQL as the backend.

### PostgreSQL

Start a PostgreSQL container:

```bash
docker run -d \
  --name lattice-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=YourPassword123! \
  -e POSTGRES_DB=lattice \
  -p 5432:5432 \
  postgres:16
```

Then modify your `docker-compose.yml` to use PostgreSQL:

```yaml
services:
  server:
    build:
      context: .
      dockerfile: docker/Dockerfile.server
    ports:
      - "8000:8000"
    environment:
      - Lattice__Database__Type=Postgres
      - Lattice__Database__Hostname=host.docker.internal
      - Lattice__Database__Port=5432
      - Lattice__Database__DatabaseName=lattice
      - Lattice__Database__Username=postgres
      - Lattice__Database__Password=YourPassword123!
```

Or include PostgreSQL in the compose file:

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=YourPassword123!
      - POSTGRES_DB=lattice
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  server:
    build:
      context: .
      dockerfile: docker/Dockerfile.server
    ports:
      - "8000:8000"
    environment:
      - Lattice__Database__Type=Postgres
      - Lattice__Database__Hostname=postgres
      - Lattice__Database__Port=5432
      - Lattice__Database__DatabaseName=lattice
      - Lattice__Database__Username=postgres
      - Lattice__Database__Password=YourPassword123!
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  postgres-data:
```

### SQL Server

Start a SQL Server container:

```bash
docker run -d \
  --name lattice-sqlserver \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD=YourPassword123! \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

Configure Lattice to use SQL Server:

```yaml
environment:
  - Lattice__Database__Type=SqlServer
  - Lattice__Database__Hostname=host.docker.internal
  - Lattice__Database__Port=1433
  - Lattice__Database__DatabaseName=lattice
  - Lattice__Database__Username=sa
  - Lattice__Database__Password=YourPassword123!
```

### MySQL

Start a MySQL container:

```bash
docker run -d \
  --name lattice-mysql \
  -e MYSQL_ROOT_PASSWORD=YourPassword123! \
  -e MYSQL_DATABASE=lattice \
  -p 3306:3306 \
  mysql:8
```

Configure Lattice to use MySQL:

```yaml
environment:
  - Lattice__Database__Type=Mysql
  - Lattice__Database__Hostname=host.docker.internal
  - Lattice__Database__Port=3306
  - Lattice__Database__DatabaseName=lattice
  - Lattice__Database__Username=root
  - Lattice__Database__Password=YourPassword123!
```

## Volumes and Data Persistence

The default compose file creates two named volumes:

| Volume | Purpose | Container Path |
|--------|---------|----------------|
| `lattice-data` | SQLite database file | `/app/data` |
| `lattice-documents` | Document storage directory | `/app/documents` |

### Backing Up Data

```bash
# Backup SQLite database
docker run --rm -v lattice-data:/data -v $(pwd):/backup alpine \
  cp /data/lattice.db /backup/lattice-backup.db

# Backup documents
docker run --rm -v lattice-documents:/documents -v $(pwd):/backup alpine \
  tar czf /backup/documents-backup.tar.gz -C /documents .
```

### Restoring Data

```bash
# Restore SQLite database
docker run --rm -v lattice-data:/data -v $(pwd):/backup alpine \
  cp /backup/lattice-backup.db /data/lattice.db

# Restore documents
docker run --rm -v lattice-documents:/documents -v $(pwd):/backup alpine \
  tar xzf /backup/documents-backup.tar.gz -C /documents
```

## Building Images

Build images individually:

```bash
# Build server image
docker build -t lattice-server -f docker/Dockerfile.server .

# Build dashboard image
docker build -t lattice-dashboard -f docker/Dockerfile.dashboard .
```

Build with docker-compose:

```bash
docker-compose build
```

## Health Checks

Both services include health checks:

- **Server**: Checks `GET /v1.0/collections` returns successfully
- **Dashboard**: Checks the nginx server responds on port 80

View health status:

```bash
docker-compose ps
```

## Logs

View logs for all services:

```bash
docker-compose logs -f
```

View logs for a specific service:

```bash
docker-compose logs -f server
docker-compose logs -f dashboard
```

## Network Configuration

The default compose file creates a bridge network. Services communicate using their service names:

- Dashboard reaches server at `http://server:8000`
- External clients reach server at `http://localhost:8000`
- External clients reach dashboard at `http://localhost:3000`

### Custom Ports

To change exposed ports, modify the `ports` section:

```yaml
services:
  server:
    ports:
      - "8080:8000"  # Server available at localhost:8080
  dashboard:
    ports:
      - "80:80"      # Dashboard available at localhost:80
```

## Production Considerations

### Security

1. **Change default passwords** - Never use example passwords in production
2. **Use secrets management** - Consider Docker secrets or environment variable injection
3. **Enable TLS** - Use a reverse proxy like Traefik or nginx with SSL certificates
4. **Restrict network access** - Use Docker networks to isolate services

### Performance

1. **Use external databases** - PostgreSQL, SQL Server, or MySQL for better performance and scalability
2. **Configure connection pooling** - External databases support connection pooling
3. **Enable horizontal scaling** - Run multiple server instances behind a load balancer

### High Availability

With an external database, you can run multiple Lattice Server instances:

```yaml
services:
  server:
    deploy:
      replicas: 3
    # ... rest of configuration
```

Use a load balancer (like Traefik, nginx, or cloud load balancers) to distribute traffic across instances.

## Troubleshooting

### Container won't start

Check logs for errors:

```bash
docker-compose logs server
```

### Database connection issues

1. Verify the database container is running and healthy
2. Check network connectivity between containers
3. Verify credentials and connection string

### Dashboard can't reach server

1. Ensure the server is healthy before dashboard starts (handled by `depends_on`)
2. Check the nginx proxy configuration
3. Verify the server is listening on port 8000 internally
