namespace Lattice.Core.Repositories.Postgresql.Queries
{
    /// <summary>
    /// SQL queries for PostgreSQL database setup.
    /// </summary>
    internal static class SetupQueries
    {
        /// <summary>
        /// Get the SQL to create all tables and indices.
        /// </summary>
        /// <returns>SQL string.</returns>
        internal static string CreateTablesAndIndices()
        {
            return @"
                -- Collections table
                CREATE TABLE IF NOT EXISTS collections (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    name VARCHAR(512) NOT NULL,
                    description TEXT,
                    documentsdirectory VARCHAR(1024),
                    schemaenforcementmode INTEGER NOT NULL DEFAULT 0,
                    indexingmode INTEGER NOT NULL DEFAULT 0,
                    createdutc TIMESTAMP NOT NULL,
                    lastupdateutc TIMESTAMP NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_collections_name ON collections(name);
                CREATE INDEX IF NOT EXISTS idx_collections_createdutc ON collections(createdutc);
                CREATE INDEX IF NOT EXISTS idx_collections_lastupdateutc ON collections(lastupdateutc);
                CREATE INDEX IF NOT EXISTS idx_collections_name_createdutc ON collections(name, createdutc);

                -- Schemas table
                CREATE TABLE IF NOT EXISTS schemas (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    name VARCHAR(512),
                    hash VARCHAR(128) NOT NULL UNIQUE,
                    createdutc TIMESTAMP NOT NULL,
                    lastupdateutc TIMESTAMP NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_schemas_hash ON schemas(hash);
                CREATE INDEX IF NOT EXISTS idx_schemas_name ON schemas(name);
                CREATE INDEX IF NOT EXISTS idx_schemas_createdutc ON schemas(createdutc);
                CREATE INDEX IF NOT EXISTS idx_schemas_lastupdateutc ON schemas(lastupdateutc);

                -- Schema elements table
                CREATE TABLE IF NOT EXISTS schemaelements (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    schemaid VARCHAR(64) NOT NULL REFERENCES schemas(id) ON DELETE CASCADE,
                    position INTEGER NOT NULL,
                    key VARCHAR(512) NOT NULL,
                    datatype VARCHAR(64) NOT NULL,
                    nullable BOOLEAN NOT NULL,
                    createdutc TIMESTAMP NOT NULL,
                    lastupdateutc TIMESTAMP NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_schemaelements_schemaid ON schemaelements(schemaid);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_key ON schemaelements(key);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_datatype ON schemaelements(datatype);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_schemaid_key ON schemaelements(schemaid, key);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_schemaid_position ON schemaelements(schemaid, position);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_key_datatype ON schemaelements(key, datatype);

                -- Documents table
                CREATE TABLE IF NOT EXISTS documents (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    collectionid VARCHAR(64) NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
                    schemaid VARCHAR(64) NOT NULL REFERENCES schemas(id),
                    name VARCHAR(512),
                    contentlength BIGINT NOT NULL DEFAULT 0,
                    sha256hash VARCHAR(128),
                    createdutc TIMESTAMP NOT NULL,
                    lastupdateutc TIMESTAMP NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_documents_collectionid ON documents(collectionid);
                CREATE INDEX IF NOT EXISTS idx_documents_schemaid ON documents(schemaid);
                CREATE INDEX IF NOT EXISTS idx_documents_name ON documents(name);
                CREATE INDEX IF NOT EXISTS idx_documents_createdutc ON documents(createdutc);
                CREATE INDEX IF NOT EXISTS idx_documents_lastupdateutc ON documents(lastupdateutc);
                CREATE INDEX IF NOT EXISTS idx_documents_collectionid_createdutc ON documents(collectionid, createdutc);
                CREATE INDEX IF NOT EXISTS idx_documents_collectionid_lastupdateutc ON documents(collectionid, lastupdateutc);
                CREATE INDEX IF NOT EXISTS idx_documents_collectionid_name ON documents(collectionid, name);
                CREATE INDEX IF NOT EXISTS idx_documents_collectionid_schemaid ON documents(collectionid, schemaid);
                CREATE INDEX IF NOT EXISTS idx_documents_schemaid_createdutc ON documents(schemaid, createdutc);

                -- Labels table (unified for collections and documents)
                CREATE TABLE IF NOT EXISTS labels (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    collectionid VARCHAR(64) REFERENCES collections(id) ON DELETE CASCADE,
                    documentid VARCHAR(64) REFERENCES documents(id) ON DELETE CASCADE,
                    labelvalue VARCHAR(512) NOT NULL,
                    createdutc TIMESTAMP NOT NULL,
                    lastupdateutc TIMESTAMP NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_labels_collectionid ON labels(collectionid);
                CREATE INDEX IF NOT EXISTS idx_labels_documentid ON labels(documentid);
                CREATE INDEX IF NOT EXISTS idx_labels_labelvalue ON labels(labelvalue);
                CREATE INDEX IF NOT EXISTS idx_labels_collectionid_labelvalue ON labels(collectionid, labelvalue);
                CREATE INDEX IF NOT EXISTS idx_labels_documentid_labelvalue ON labels(documentid, labelvalue);
                CREATE INDEX IF NOT EXISTS idx_labels_labelvalue_documentid ON labels(labelvalue, documentid);
                CREATE INDEX IF NOT EXISTS idx_labels_createdutc ON labels(createdutc);

                -- Tags table (unified for collections and documents)
                CREATE TABLE IF NOT EXISTS tags (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    collectionid VARCHAR(64) REFERENCES collections(id) ON DELETE CASCADE,
                    documentid VARCHAR(64) REFERENCES documents(id) ON DELETE CASCADE,
                    key VARCHAR(256) NOT NULL,
                    value TEXT,
                    createdutc TIMESTAMP NOT NULL,
                    lastupdateutc TIMESTAMP NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_tags_collectionid ON tags(collectionid);
                CREATE INDEX IF NOT EXISTS idx_tags_documentid ON tags(documentid);
                CREATE INDEX IF NOT EXISTS idx_tags_key ON tags(key);
                CREATE INDEX IF NOT EXISTS idx_tags_collectionid_key ON tags(collectionid, key);
                CREATE INDEX IF NOT EXISTS idx_tags_documentid_key ON tags(documentid, key);
                CREATE INDEX IF NOT EXISTS idx_tags_createdutc ON tags(createdutc);

                -- Index table mappings
                CREATE TABLE IF NOT EXISTS indextablemappings (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    key VARCHAR(512) NOT NULL UNIQUE,
                    tablename VARCHAR(256) NOT NULL UNIQUE,
                    createdutc TIMESTAMP NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_indextablemappings_key ON indextablemappings(key);
                CREATE INDEX IF NOT EXISTS idx_indextablemappings_tablename ON indextablemappings(tablename);
                CREATE INDEX IF NOT EXISTS idx_indextablemappings_createdutc ON indextablemappings(createdutc);

                -- Field constraints table (schema constraints for collections)
                CREATE TABLE IF NOT EXISTS fieldconstraints (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    collectionid VARCHAR(64) NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
                    fieldpath VARCHAR(512) NOT NULL,
                    datatype VARCHAR(64),
                    required BOOLEAN NOT NULL DEFAULT FALSE,
                    nullable BOOLEAN NOT NULL DEFAULT TRUE,
                    regexpattern VARCHAR(1024),
                    minvalue DECIMAL(18,6),
                    maxvalue DECIMAL(18,6),
                    minlength INTEGER,
                    maxlength INTEGER,
                    allowedvalues TEXT,
                    arrayelementtype VARCHAR(64),
                    createdutc TIMESTAMP NOT NULL,
                    lastupdateutc TIMESTAMP NOT NULL,
                    UNIQUE(collectionid, fieldpath)
                );

                CREATE INDEX IF NOT EXISTS idx_fieldconstraints_collectionid ON fieldconstraints(collectionid);

                -- Indexed fields table (selective indexing configuration)
                CREATE TABLE IF NOT EXISTS indexedfields (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    collectionid VARCHAR(64) NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
                    fieldpath VARCHAR(512) NOT NULL,
                    createdutc TIMESTAMP NOT NULL,
                    lastupdateutc TIMESTAMP NOT NULL,
                    UNIQUE(collectionid, fieldpath)
                );

                CREATE INDEX IF NOT EXISTS idx_indexedfields_collectionid ON indexedfields(collectionid);

                -- Object locks table (distributed locking for document ingestion)
                CREATE TABLE IF NOT EXISTS objectlocks (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    collectionid VARCHAR(64) NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
                    documentname VARCHAR(512) NOT NULL,
                    hostname VARCHAR(256) NOT NULL,
                    createdutc TIMESTAMP NOT NULL,
                    UNIQUE(collectionid, documentname)
                );

                CREATE INDEX IF NOT EXISTS idx_objectlocks_createdutc ON objectlocks(createdutc);
                CREATE INDEX IF NOT EXISTS idx_objectlocks_hostname ON objectlocks(hostname);
            ";
        }

        /// <summary>
        /// Get migration statements to add new columns to existing tables.
        /// Each statement is returned separately so errors can be caught individually.
        /// </summary>
        /// <returns>Array of SQL statements.</returns>
        internal static string[] GetMigrationStatements()
        {
            return new[]
            {
                "ALTER TABLE documents ADD COLUMN IF NOT EXISTS contentlength BIGINT DEFAULT 0;",
                "ALTER TABLE documents ADD COLUMN IF NOT EXISTS sha256hash VARCHAR(128);"
            };
        }

        /// <summary>
        /// Get the SQL to create a dynamic index table.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>SQL string.</returns>
        internal static string CreateIndexTable(string tableName)
        {
            return $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    id VARCHAR(64) NOT NULL PRIMARY KEY,
                    documentid VARCHAR(64) NOT NULL,
                    position INTEGER,
                    value TEXT,
                    createdutc TIMESTAMP NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_{tableName}_documentid ON {tableName}(documentid);
                CREATE INDEX IF NOT EXISTS idx_{tableName}_position ON {tableName}(position);
                CREATE INDEX IF NOT EXISTS idx_{tableName}_createdutc ON {tableName}(createdutc);
                CREATE INDEX IF NOT EXISTS idx_{tableName}_documentid_position ON {tableName}(documentid, position);
            ";
        }

        /// <summary>
        /// Get the SQL to drop a dynamic index table.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>SQL string.</returns>
        internal static string DropIndexTable(string tableName)
        {
            return $"DROP TABLE IF EXISTS {tableName};";
        }
    }
}
