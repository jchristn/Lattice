namespace Lattice.Core.Repositories.SqlServer.Queries
{
    /// <summary>
    /// SQL queries for SQL Server database setup.
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
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'collections')
                CREATE TABLE [collections] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [name] NVARCHAR(512) NOT NULL,
                    [description] NVARCHAR(MAX),
                    [documentsdirectory] NVARCHAR(1024),
                    [schemaenforcementmode] INT NOT NULL DEFAULT 0,
                    [indexingmode] INT NOT NULL DEFAULT 0,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_collections_name')
                CREATE INDEX [idx_collections_name] ON [collections]([name]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_collections_createdutc')
                CREATE INDEX [idx_collections_createdutc] ON [collections]([createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_collections_lastupdateutc')
                CREATE INDEX [idx_collections_lastupdateutc] ON [collections]([lastupdateutc]);

                -- Schemas table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'schemas')
                CREATE TABLE [schemas] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [name] NVARCHAR(512),
                    [hash] NVARCHAR(128) NOT NULL UNIQUE,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_schemas_hash')
                CREATE INDEX [idx_schemas_hash] ON [schemas]([hash]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_schemas_name')
                CREATE INDEX [idx_schemas_name] ON [schemas]([name]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_schemas_createdutc')
                CREATE INDEX [idx_schemas_createdutc] ON [schemas]([createdutc]);

                -- Schema elements table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'schemaelements')
                CREATE TABLE [schemaelements] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [schemaid] NVARCHAR(64) NOT NULL,
                    [position] INT NOT NULL,
                    [key] NVARCHAR(512) NOT NULL,
                    [datatype] NVARCHAR(64) NOT NULL,
                    [nullable] BIT NOT NULL,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_schemaelements_schemas] FOREIGN KEY ([schemaid]) REFERENCES [schemas]([id]) ON DELETE CASCADE
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_schemaelements_schemaid')
                CREATE INDEX [idx_schemaelements_schemaid] ON [schemaelements]([schemaid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_schemaelements_key')
                CREATE INDEX [idx_schemaelements_key] ON [schemaelements]([key]);

                -- Documents table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'documents')
                CREATE TABLE [documents] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [collectionid] NVARCHAR(64) NOT NULL,
                    [schemaid] NVARCHAR(64) NOT NULL,
                    [name] NVARCHAR(512),
                    [contentlength] BIGINT NOT NULL DEFAULT 0,
                    [sha256hash] NVARCHAR(128),
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_documents_collections] FOREIGN KEY ([collectionid]) REFERENCES [collections]([id]) ON DELETE CASCADE,
                    CONSTRAINT [fk_documents_schemas] FOREIGN KEY ([schemaid]) REFERENCES [schemas]([id])
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_documents_collectionid')
                CREATE INDEX [idx_documents_collectionid] ON [documents]([collectionid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_documents_schemaid')
                CREATE INDEX [idx_documents_schemaid] ON [documents]([schemaid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_documents_name')
                CREATE INDEX [idx_documents_name] ON [documents]([name]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_documents_createdutc')
                CREATE INDEX [idx_documents_createdutc] ON [documents]([createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_documents_collectionid_createdutc')
                CREATE INDEX [idx_documents_collectionid_createdutc] ON [documents]([collectionid], [createdutc]);

                -- Labels table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'labels')
                CREATE TABLE [labels] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [collectionid] NVARCHAR(64),
                    [documentid] NVARCHAR(64),
                    [labelvalue] NVARCHAR(512) NOT NULL,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_labels_collections] FOREIGN KEY ([collectionid]) REFERENCES [collections]([id]) ON DELETE CASCADE
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_labels_collectionid')
                CREATE INDEX [idx_labels_collectionid] ON [labels]([collectionid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_labels_documentid')
                CREATE INDEX [idx_labels_documentid] ON [labels]([documentid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_labels_labelvalue')
                CREATE INDEX [idx_labels_labelvalue] ON [labels]([labelvalue]);

                -- Tags table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tags')
                CREATE TABLE [tags] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [collectionid] NVARCHAR(64),
                    [documentid] NVARCHAR(64),
                    [key] NVARCHAR(256) NOT NULL,
                    [value] NVARCHAR(MAX),
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_tags_collections] FOREIGN KEY ([collectionid]) REFERENCES [collections]([id]) ON DELETE CASCADE
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_tags_collectionid')
                CREATE INDEX [idx_tags_collectionid] ON [tags]([collectionid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_tags_documentid')
                CREATE INDEX [idx_tags_documentid] ON [tags]([documentid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_tags_key')
                CREATE INDEX [idx_tags_key] ON [tags]([key]);

                -- Index table mappings
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'indextablemappings')
                CREATE TABLE [indextablemappings] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [key] NVARCHAR(512) NOT NULL UNIQUE,
                    [tablename] NVARCHAR(256) NOT NULL UNIQUE,
                    [createdutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_indextablemappings_key')
                CREATE INDEX [idx_indextablemappings_key] ON [indextablemappings]([key]);

                -- Field constraints table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'fieldconstraints')
                CREATE TABLE [fieldconstraints] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [collectionid] NVARCHAR(64) NOT NULL,
                    [fieldpath] NVARCHAR(512) NOT NULL,
                    [datatype] NVARCHAR(64),
                    [required] BIT NOT NULL DEFAULT 0,
                    [nullable] BIT NOT NULL DEFAULT 1,
                    [regexpattern] NVARCHAR(1024),
                    [minvalue] DECIMAL(18,6),
                    [maxvalue] DECIMAL(18,6),
                    [minlength] INT,
                    [maxlength] INT,
                    [allowedvalues] NVARCHAR(MAX),
                    [arrayelementtype] NVARCHAR(64),
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_fieldconstraints_collections] FOREIGN KEY ([collectionid]) REFERENCES [collections]([id]) ON DELETE CASCADE,
                    CONSTRAINT [uk_fieldconstraints_collectionid_fieldpath] UNIQUE ([collectionid], [fieldpath])
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_fieldconstraints_collectionid')
                CREATE INDEX [idx_fieldconstraints_collectionid] ON [fieldconstraints]([collectionid]);

                -- Indexed fields table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'indexedfields')
                CREATE TABLE [indexedfields] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [collectionid] NVARCHAR(64) NOT NULL,
                    [fieldpath] NVARCHAR(512) NOT NULL,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_indexedfields_collections] FOREIGN KEY ([collectionid]) REFERENCES [collections]([id]) ON DELETE CASCADE,
                    CONSTRAINT [uk_indexedfields_collectionid_fieldpath] UNIQUE ([collectionid], [fieldpath])
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_indexedfields_collectionid')
                CREATE INDEX [idx_indexedfields_collectionid] ON [indexedfields]([collectionid]);

                -- Object locks table (distributed locking for document ingestion)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'objectlocks')
                CREATE TABLE [objectlocks] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [collectionid] NVARCHAR(64) NOT NULL,
                    [documentname] NVARCHAR(512) NOT NULL,
                    [hostname] NVARCHAR(256) NOT NULL,
                    [createdutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_objectlocks_collections] FOREIGN KEY ([collectionid]) REFERENCES [collections]([id]) ON DELETE CASCADE,
                    CONSTRAINT [uk_objectlocks_collectionid_documentname] UNIQUE ([collectionid], [documentname])
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_objectlocks_createdutc')
                CREATE INDEX [idx_objectlocks_createdutc] ON [objectlocks]([createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_objectlocks_hostname')
                CREATE INDEX [idx_objectlocks_hostname] ON [objectlocks]([hostname]);
            ";
        }

        /// <summary>
        /// Get migration statements to add new columns to existing tables.
        /// </summary>
        /// <returns>Array of SQL statements.</returns>
        internal static string[] GetMigrationStatements()
        {
            return new[]
            {
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('documents') AND name = 'contentlength') ALTER TABLE [documents] ADD [contentlength] BIGINT DEFAULT 0;",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('documents') AND name = 'sha256hash') ALTER TABLE [documents] ADD [sha256hash] NVARCHAR(128);"
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
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}')
                CREATE TABLE [{tableName}] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [documentid] NVARCHAR(64) NOT NULL,
                    [position] INT,
                    [value] NVARCHAR(MAX),
                    [createdutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_{tableName}_documentid')
                CREATE INDEX [idx_{tableName}_documentid] ON [{tableName}]([documentid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_{tableName}_position')
                CREATE INDEX [idx_{tableName}_position] ON [{tableName}]([position]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_{tableName}_createdutc')
                CREATE INDEX [idx_{tableName}_createdutc] ON [{tableName}]([createdutc]);
            ";
        }

        /// <summary>
        /// Get the SQL to drop a dynamic index table.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>SQL string.</returns>
        internal static string DropIndexTable(string tableName)
        {
            return $"IF EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}') DROP TABLE [{tableName}];";
        }
    }
}
