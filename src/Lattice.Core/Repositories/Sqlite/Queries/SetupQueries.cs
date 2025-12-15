namespace Lattice.Core.Repositories.Sqlite.Queries
{
    /// <summary>
    /// SQL queries for database setup.
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
                    id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    description TEXT,
                    documentsdirectory TEXT,
                    schemaenforcementmode INTEGER NOT NULL DEFAULT 0,
                    indexingmode INTEGER NOT NULL DEFAULT 0,
                    createdutc TEXT NOT NULL,
                    lastupdateutc TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_collections_name ON collections(name);
                CREATE INDEX IF NOT EXISTS idx_collections_createdutc ON collections(createdutc);
                CREATE INDEX IF NOT EXISTS idx_collections_lastupdateutc ON collections(lastupdateutc);
                CREATE INDEX IF NOT EXISTS idx_collections_name_createdutc ON collections(name, createdutc);

                -- Schemas table
                CREATE TABLE IF NOT EXISTS schemas (
                    id TEXT PRIMARY KEY,
                    name TEXT,
                    hash TEXT NOT NULL UNIQUE,
                    createdutc TEXT NOT NULL,
                    lastupdateutc TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_schemas_hash ON schemas(hash);
                CREATE INDEX IF NOT EXISTS idx_schemas_name ON schemas(name);
                CREATE INDEX IF NOT EXISTS idx_schemas_createdutc ON schemas(createdutc);
                CREATE INDEX IF NOT EXISTS idx_schemas_lastupdateutc ON schemas(lastupdateutc);

                -- Schema elements table
                CREATE TABLE IF NOT EXISTS schemaelements (
                    id TEXT PRIMARY KEY,
                    schemaid TEXT NOT NULL,
                    position INTEGER NOT NULL,
                    key TEXT NOT NULL,
                    datatype TEXT NOT NULL,
                    nullable INTEGER NOT NULL,
                    createdutc TEXT NOT NULL,
                    lastupdateutc TEXT NOT NULL,
                    FOREIGN KEY (schemaid) REFERENCES schemas(id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS idx_schemaelements_schemaid ON schemaelements(schemaid);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_key ON schemaelements(key);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_datatype ON schemaelements(datatype);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_schemaid_key ON schemaelements(schemaid, key);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_schemaid_position ON schemaelements(schemaid, position);
                CREATE INDEX IF NOT EXISTS idx_schemaelements_key_datatype ON schemaelements(key, datatype);

                -- Documents table
                CREATE TABLE IF NOT EXISTS documents (
                    id TEXT PRIMARY KEY,
                    collectionid TEXT NOT NULL,
                    schemaid TEXT NOT NULL,
                    name TEXT,
                    contentlength INTEGER NOT NULL DEFAULT 0,
                    sha256hash TEXT,
                    createdutc TEXT NOT NULL,
                    lastupdateutc TEXT NOT NULL,
                    FOREIGN KEY (collectionid) REFERENCES collections(id) ON DELETE CASCADE,
                    FOREIGN KEY (schemaid) REFERENCES schemas(id)
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
                    id TEXT PRIMARY KEY,
                    collectionid TEXT,
                    documentid TEXT,
                    labelvalue TEXT NOT NULL,
                    createdutc TEXT NOT NULL,
                    lastupdateutc TEXT NOT NULL,
                    FOREIGN KEY (collectionid) REFERENCES collections(id) ON DELETE CASCADE,
                    FOREIGN KEY (documentid) REFERENCES documents(id) ON DELETE CASCADE
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
                    id TEXT PRIMARY KEY,
                    collectionid TEXT,
                    documentid TEXT,
                    key TEXT NOT NULL,
                    value TEXT,
                    createdutc TEXT NOT NULL,
                    lastupdateutc TEXT NOT NULL,
                    FOREIGN KEY (collectionid) REFERENCES collections(id) ON DELETE CASCADE,
                    FOREIGN KEY (documentid) REFERENCES documents(id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS idx_tags_collectionid ON tags(collectionid);
                CREATE INDEX IF NOT EXISTS idx_tags_documentid ON tags(documentid);
                CREATE INDEX IF NOT EXISTS idx_tags_key ON tags(key);
                CREATE INDEX IF NOT EXISTS idx_tags_value ON tags(value);
                CREATE INDEX IF NOT EXISTS idx_tags_collectionid_key ON tags(collectionid, key);
                CREATE INDEX IF NOT EXISTS idx_tags_documentid_key ON tags(documentid, key);
                CREATE INDEX IF NOT EXISTS idx_tags_key_value ON tags(key, value);
                CREATE INDEX IF NOT EXISTS idx_tags_collectionid_key_value ON tags(collectionid, key, value);
                CREATE INDEX IF NOT EXISTS idx_tags_documentid_key_value ON tags(documentid, key, value);
                CREATE INDEX IF NOT EXISTS idx_tags_key_value_documentid ON tags(key, value, documentid);
                CREATE INDEX IF NOT EXISTS idx_tags_createdutc ON tags(createdutc);

                -- Index table mappings
                CREATE TABLE IF NOT EXISTS indextablemappings (
                    id TEXT PRIMARY KEY,
                    key TEXT NOT NULL UNIQUE,
                    tablename TEXT NOT NULL UNIQUE,
                    createdutc TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_indextablemappings_key ON indextablemappings(key);
                CREATE INDEX IF NOT EXISTS idx_indextablemappings_tablename ON indextablemappings(tablename);
                CREATE INDEX IF NOT EXISTS idx_indextablemappings_createdutc ON indextablemappings(createdutc);

                -- Field constraints table (schema constraints for collections)
                CREATE TABLE IF NOT EXISTS fieldconstraints (
                    id TEXT PRIMARY KEY,
                    collectionid TEXT NOT NULL,
                    fieldpath TEXT NOT NULL,
                    datatype TEXT,
                    required INTEGER NOT NULL DEFAULT 0,
                    nullable INTEGER NOT NULL DEFAULT 1,
                    regexpattern TEXT,
                    minvalue REAL,
                    maxvalue REAL,
                    minlength INTEGER,
                    maxlength INTEGER,
                    allowedvalues TEXT,
                    arrayelementtype TEXT,
                    createdutc TEXT NOT NULL,
                    lastupdateutc TEXT NOT NULL,
                    FOREIGN KEY (collectionid) REFERENCES collections(id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS idx_fieldconstraints_collectionid ON fieldconstraints(collectionid);
                CREATE UNIQUE INDEX IF NOT EXISTS idx_fieldconstraints_collectionid_fieldpath ON fieldconstraints(collectionid, fieldpath);

                -- Indexed fields table (selective indexing configuration)
                CREATE TABLE IF NOT EXISTS indexedfields (
                    id TEXT PRIMARY KEY,
                    collectionid TEXT NOT NULL,
                    fieldpath TEXT NOT NULL,
                    createdutc TEXT NOT NULL,
                    lastupdateutc TEXT NOT NULL,
                    FOREIGN KEY (collectionid) REFERENCES collections(id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS idx_indexedfields_collectionid ON indexedfields(collectionid);
                CREATE UNIQUE INDEX IF NOT EXISTS idx_indexedfields_collectionid_fieldpath ON indexedfields(collectionid, fieldpath);

                -- Object locks table (distributed locking for document ingestion)
                CREATE TABLE IF NOT EXISTS objectlocks (
                    id TEXT PRIMARY KEY,
                    collectionid TEXT NOT NULL,
                    documentname TEXT NOT NULL,
                    hostname TEXT NOT NULL,
                    createdutc TEXT NOT NULL,
                    FOREIGN KEY (collectionid) REFERENCES collections(id) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS idx_objectlocks_collectionid_documentname ON objectlocks(collectionid, documentname);
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
                // Add contentlength column to documents table (use DEFAULT 0 for existing rows)
                "ALTER TABLE documents ADD COLUMN contentlength INTEGER DEFAULT 0;",
                // Add sha256hash column to documents table
                "ALTER TABLE documents ADD COLUMN sha256hash TEXT;"
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
                    id TEXT PRIMARY KEY,
                    documentid TEXT NOT NULL,
                    position INTEGER,
                    value TEXT,
                    createdutc TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_{tableName}_documentid ON {tableName}(documentid);
                CREATE INDEX IF NOT EXISTS idx_{tableName}_value ON {tableName}(value);
                CREATE INDEX IF NOT EXISTS idx_{tableName}_position ON {tableName}(position);
                CREATE INDEX IF NOT EXISTS idx_{tableName}_createdutc ON {tableName}(createdutc);
                CREATE INDEX IF NOT EXISTS idx_{tableName}_documentid_value ON {tableName}(documentid, value);
                CREATE INDEX IF NOT EXISTS idx_{tableName}_documentid_position ON {tableName}(documentid, position);
                CREATE INDEX IF NOT EXISTS idx_{tableName}_value_documentid ON {tableName}(value, documentid);
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
