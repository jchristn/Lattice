# Lattice: A Comparative Analysis

This document provides a factual analysis of Lattice, articulating where it fits in the database landscape and how it compares to existing solutions.

---

## Table of Contents

1. [What is Lattice?](#what-is-lattice)
2. [The Problem Lattice Solves](#the-problem-lattice-solves)
3. [Core Architecture](#core-architecture)
4. [Comparison with NoSQL Databases](#comparison-with-nosql-databases)
5. [Comparison with SQL Databases](#comparison-with-sql-databases)
6. [Feature Summary](#feature-summary)
7. [Use Case Matrix](#use-case-matrix)
8. [Decision Framework](#decision-framework)

---

## What is Lattice?

Lattice is a JSON document store with automatic schema detection, SQL-like querying, and flexible indexing.

| Characteristic | Description |
|----------------|-------------|
| **Storage Model** | JSON documents stored as files, metadata and indexes in relational database |
| **Database Backends** | SQLite, SQL Server, PostgreSQL, MySQL |
| **Query Language** | SQL-like DSL (`WHERE field = 'value' AND age > 30`) |
| **Schema** | Automatic detection with optional enforcement |
| **Indexing** | Dynamic per-field index tables with selective indexing |
| **Deployment** | Embedded, single-node, or horizontally scaled with shared database |
| **Platform** | .NET (C#) with HTTP REST interface |

---

## The Problem Lattice Solves

Traditional databases operate in one of two paradigms:

**Relational Databases (SQL):**
- Require schema definition upfront
- Schema changes need migrations
- Heterogeneous data requires JSONB columns or EAV patterns

**Document Databases (NoSQL):**
- Schema-free flexibility
- Different query languages (MQL, Mango, Query DSL)
- May require separate indexing configuration

**Lattice's position:**

```
+-----------------------------------------------------------------------+
|                            Schema Rigidity                            |
|  Flexible                                                    Rigid    |
|  <----------------------------------------------------------------->  |
|                                                                       |
|  MongoDB     CouchDB        Lattice    PostgreSQL JSONB      MySQL    |
|  DynamoDB    Elasticsearch                                            |
|                               ^                                       |
|                               |                                       |
|                        Optional Schema                                |
|                        Enforcement                                    |
+-----------------------------------------------------------------------+
```

**Lattice's Approach:**

1. **Zero-schema start**: Ingest JSON without defining structure upfront
2. **SQL-like queries**: Uses familiar `WHERE field = 'value'` syntax
3. **Optional enforcement**: Schema constraints can be added at any time
4. **Automatic indexing**: Fields indexed by default with selective override
5. **Single-process deployment**: No cluster configuration required

---

## Core Architecture

```
+----------------------------------------------------------------------+
|                         Lattice Architecture                         |
+----------------------------------------------------------------------+
|                                                                      |
|   JSON Document Input                                                |
|          |                                                           |
|          v                                                           |
|   +------------------+       +----------------------------------+    |
|   | Schema Generator |------>| schemas + schemaelements tables  |    |
|   | (auto-detect)    |       | (deduplicated by SHA256 hash)    |    |
|   +------------------+       +----------------------------------+    |
|          |                                                           |
|          v                                                           |
|   +------------------+       +----------------------------------+    |
|   | JSON Flattener   |------>| Dynamic Index Tables             |    |
|   | (dot-notation)   |       | (one table per unique field)     |    |
|   +------------------+       +----------------------------------+    |
|          |                                                           |
|          v                                                           |
|   +------------------+                                               |
|   | File System      |                                               |
|   | {doc_id}.json    |<---- Raw JSON preserved on disk               |
|   +------------------+                                               |
|                                                                      |
+----------------------------------------------------------------------+
```

### Design Decisions

| Decision | Rationale | Trade-off |
|----------|-----------|-----------|
| One index table per field | Sparse data efficiency | Many tables with large schemas |
| Pluggable database backend | Flexibility (SQLite for embedded, PostgreSQL/SQL Server/MySQL for scale) | Configuration complexity for server databases |
| Raw JSON on disk | Original fidelity preserved | Disk I/O for content retrieval |
| Automatic schema detection | No upfront definition | Schema storage overhead |
| Hash-based schema deduplication | Storage efficiency | Hash computation cost |

---

## Comparison with NoSQL Databases

### MongoDB

| Aspect | MongoDB | Lattice |
|--------|---------|---------|
| Query Language | MQL, Aggregation Pipeline | SQL-like DSL |
| Scalability | Horizontal sharding | Horizontal via shared database (PostgreSQL/SQL Server/MySQL) |
| Indexing | Manual creation | Automatic |
| Full-text Search | Supported | Not supported |
| Aggregations | Supported | Not supported |
| Transactions | Multi-document ACID | Single-document |
| Deployment | Server/cluster | Embedded, REST, or multi-instance |

### Elasticsearch

| Aspect | Elasticsearch | Lattice |
|--------|---------------|---------|
| Primary Purpose | Full-text search, analytics | Document storage with querying |
| Full-text Search | Supported | Not supported |
| Aggregations | Supported | Not supported |
| Scalability | Distributed | Horizontal via shared database backend |
| Consistency | Eventually consistent | Immediately consistent |

### CouchDB

| Aspect | CouchDB | Lattice |
|--------|---------|---------|
| Replication | Master-master with sync | Not supported |
| Indexing | Manual view creation | Automatic |
| Binary Attachments | Supported | Not supported |
| Offline-first | Built-in | Not supported |

### DynamoDB

| Aspect | DynamoDB | Lattice |
|--------|----------|---------|
| Hosting | AWS managed | Self-hosted |
| Query Model | Primary key + secondary indexes | Any field via SQL-like DSL |
| Scalability | Auto-scaling | Horizontal via shared database backend |
| Indexing | Manual GSI/LSI creation | Automatic |

---

## Comparison with SQL Databases

### PostgreSQL

| Aspect | PostgreSQL | Lattice |
|--------|------------|---------|
| Schema | Required upfront | Automatic detection |
| JSON Indexing | Manual GIN index creation | Automatic per-field |
| JOINs | Supported | Not supported |
| Aggregations | Supported | Not supported |
| Transactions | Multi-statement ACID | Single-document |

**PostgreSQL JSON querying:**
```sql
CREATE INDEX idx_email ON users USING GIN ((data->'email'));
SELECT * FROM users WHERE data->>'email' = 'user@example.com';
```

**Lattice equivalent:**
```sql
WHERE email = 'user@example.com'
```

### MySQL

| Aspect | MySQL | Lattice |
|--------|-------|---------|
| JSON Indexing | Generated columns + indexes | Automatic per-field |
| Schema | Required, ALTER TABLE | Automatic detection |
| Replication | Supported | Not supported |

### SQLite

Lattice supports SQLite as one of its storage backends (along with SQL Server, PostgreSQL, and MySQL).

| Aspect | SQLite Direct | Lattice (SQLite backend) |
|--------|---------------|---------|
| JSON Indexing | Manual expression indexes | Automatic per-field tables |
| Schema | Required upfront | Automatic detection |
| Abstraction | Direct table access | Document store abstraction |
| Scaling | Single-node | Single-node (use PostgreSQL/SQL Server/MySQL for horizontal scaling) |

**SQLite JSON querying:**
```sql
SELECT * FROM documents WHERE json_extract(data, '$.email') = 'user@example.com';
```

**Lattice equivalent:**
```sql
WHERE email = 'user@example.com'
```

---

## Feature Summary

### What Lattice Provides

- **Multiple database backends**: SQLite, SQL Server, PostgreSQL, MySQL
- **Horizontal scaling**: Multiple Lattice instances sharing a common database
- Automatic schema detection from JSON on ingestion
- Schema deduplication via SHA256 hash matching
- Per-field index tables created automatically
- SQL-like queries: `=`, `!=`, `>`, `>=`, `<`, `<=`, `LIKE`, `IS NULL`, `IS NOT NULL`
- Schema enforcement modes: None, Strict, Flexible, Partial
- Field constraints: type, required, nullable, regex, min/max, allowed values
- Indexing modes: All (default), Selective, None
- Labels and tags for metadata organization
- REST API via Lattice.Server
- Embedded .NET library use

### What Lattice Does Not Provide

- Automatic replication (rely on database backend replication)
- Full-text search (exact match and LIKE only)
- Aggregations (no SUM, COUNT, AVG, GROUP BY)
- JOINs
- Multi-document transactions
- Change streams / real-time notifications
- Binary storage (JSON only)
- OR queries (AND only)

---

## Use Case Matrix

| Use Case | Lattice | MongoDB | Elasticsearch | PostgreSQL | SQLite |
|----------|:-------:|:-------:|:-------------:|:----------:|:------:|
| Rapid prototyping | Yes | Yes | Partial | Partial | Yes |
| Heterogeneous JSON | Yes | Yes | Yes | Partial | Partial |
| Full-text search | No | Partial | Yes | Yes | Partial |
| Aggregations | No | Yes | Yes | Yes | Yes |
| Embedded/Desktop | Yes | No | No | No | Yes |
| Horizontal scaling | Yes* | Yes | Yes | Partial | No |
| Multi-doc transactions | No | Yes | No | Yes | Yes |
| Relational data | No | No | No | Yes | Yes |

*Horizontal scaling supported with PostgreSQL, SQL Server, or MySQL backends

---

## Decision Framework

### Lattice is applicable when:

1. Building embedded or desktop applications requiring local document storage (SQLite backend)
2. Building scalable web services with multiple instances (PostgreSQL/SQL Server/MySQL backend)
3. Schema is evolving and migration overhead is undesirable
4. Ingesting JSON from multiple sources with varying structures
5. SQL-like query syntax is preferred
6. Automatic field indexing is desired
7. Optional schema enforcement is needed
8. Horizontal scaling via shared database is acceptable

### Lattice is not applicable when:

1. Full-text search with relevance scoring is needed
2. Aggregations (SUM, COUNT, GROUP BY) are required
3. Data is relational with JOIN requirements
4. Built-in sharding is required (Lattice relies on database-level scaling)
5. Real-time change notifications are required
6. Binary data storage is needed

---

## Summary

Lattice is a JSON document store with automatic schema detection, SQL-like querying, and configurable indexing. It supports multiple database backends:

- **SQLite**: Embedded single-node deployment, zero configuration
- **PostgreSQL / SQL Server / MySQL**: Shared database enabling horizontal scaling across multiple Lattice instances

**Provides:** Automatic schema detection and field indexing, SQL-like query syntax, optional schema enforcement, flexible deployment from embedded to horizontally scaled.

**Does not provide:** Built-in replication (use database replication), full-text search, aggregations, JOINs, multi-document transactions, binary storage.
