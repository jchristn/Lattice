namespace Lattice.Core.Repositories.Mysql.Queries
{
    /// <summary>
    /// SQL queries for MySQL database setup.
    /// </summary>
    internal static class SetupQueries
    {
        /// <summary>
        /// Get the SQL to create all tables and indices.
        /// </summary>
        /// <param name="database">The database name for foreign key references.</param>
        /// <returns>SQL string.</returns>
        internal static string CreateTablesAndIndices(string database)
        {
            return $@"
                -- Collections table
                CREATE TABLE IF NOT EXISTS `collections` (
                    `id` VARCHAR(64) NOT NULL,
                    `name` VARCHAR(512) NOT NULL,
                    `description` TEXT,
                    `documentsdirectory` VARCHAR(1024),
                    `schemaenforcementmode` INT NOT NULL DEFAULT 0,
                    `indexingmode` INT NOT NULL DEFAULT 0,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_collections_name` ON `collections`(`name`(255));
                CREATE INDEX IF NOT EXISTS `idx_collections_createdutc` ON `collections`(`createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_collections_lastupdateutc` ON `collections`(`lastupdateutc`);
                CREATE INDEX IF NOT EXISTS `idx_collections_name_createdutc` ON `collections`(`name`(255), `createdutc`);

                -- Schemas table
                CREATE TABLE IF NOT EXISTS `schemas` (
                    `id` VARCHAR(64) NOT NULL,
                    `name` VARCHAR(512),
                    `hash` VARCHAR(128) NOT NULL,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    UNIQUE KEY `uk_schemas_hash` (`hash`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_schemas_hash` ON `schemas`(`hash`);
                CREATE INDEX IF NOT EXISTS `idx_schemas_name` ON `schemas`(`name`(255));
                CREATE INDEX IF NOT EXISTS `idx_schemas_createdutc` ON `schemas`(`createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_schemas_lastupdateutc` ON `schemas`(`lastupdateutc`);

                -- Schema elements table
                CREATE TABLE IF NOT EXISTS `schemaelements` (
                    `id` VARCHAR(64) NOT NULL,
                    `schemaid` VARCHAR(64) NOT NULL,
                    `position` INT NOT NULL,
                    `key` VARCHAR(512) NOT NULL,
                    `datatype` VARCHAR(64) NOT NULL,
                    `nullable` TINYINT NOT NULL,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_schemaelements_schemas` FOREIGN KEY (`schemaid`) REFERENCES `schemas`(`id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_schemaelements_schemaid` ON `schemaelements`(`schemaid`);
                CREATE INDEX IF NOT EXISTS `idx_schemaelements_key` ON `schemaelements`(`key`(255));
                CREATE INDEX IF NOT EXISTS `idx_schemaelements_datatype` ON `schemaelements`(`datatype`);
                CREATE INDEX IF NOT EXISTS `idx_schemaelements_schemaid_key` ON `schemaelements`(`schemaid`, `key`(255));
                CREATE INDEX IF NOT EXISTS `idx_schemaelements_schemaid_position` ON `schemaelements`(`schemaid`, `position`);
                CREATE INDEX IF NOT EXISTS `idx_schemaelements_key_datatype` ON `schemaelements`(`key`(255), `datatype`);

                -- Documents table
                CREATE TABLE IF NOT EXISTS `documents` (
                    `id` VARCHAR(64) NOT NULL,
                    `collectionid` VARCHAR(64) NOT NULL,
                    `schemaid` VARCHAR(64) NOT NULL,
                    `name` VARCHAR(512),
                    `contentlength` BIGINT NOT NULL DEFAULT 0,
                    `sha256hash` VARCHAR(128),
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_documents_collections` FOREIGN KEY (`collectionid`) REFERENCES `collections`(`id`) ON DELETE CASCADE,
                    CONSTRAINT `fk_documents_schemas` FOREIGN KEY (`schemaid`) REFERENCES `schemas`(`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_documents_collectionid` ON `documents`(`collectionid`);
                CREATE INDEX IF NOT EXISTS `idx_documents_schemaid` ON `documents`(`schemaid`);
                CREATE INDEX IF NOT EXISTS `idx_documents_name` ON `documents`(`name`(255));
                CREATE INDEX IF NOT EXISTS `idx_documents_createdutc` ON `documents`(`createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_documents_lastupdateutc` ON `documents`(`lastupdateutc`);
                CREATE INDEX IF NOT EXISTS `idx_documents_collectionid_createdutc` ON `documents`(`collectionid`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_documents_collectionid_lastupdateutc` ON `documents`(`collectionid`, `lastupdateutc`);
                CREATE INDEX IF NOT EXISTS `idx_documents_collectionid_name` ON `documents`(`collectionid`, `name`(255));
                CREATE INDEX IF NOT EXISTS `idx_documents_collectionid_schemaid` ON `documents`(`collectionid`, `schemaid`);
                CREATE INDEX IF NOT EXISTS `idx_documents_schemaid_createdutc` ON `documents`(`schemaid`, `createdutc`);

                -- Labels table (unified for collections and documents)
                CREATE TABLE IF NOT EXISTS `labels` (
                    `id` VARCHAR(64) NOT NULL,
                    `collectionid` VARCHAR(64),
                    `documentid` VARCHAR(64),
                    `labelvalue` VARCHAR(512) NOT NULL,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_labels_collections` FOREIGN KEY (`collectionid`) REFERENCES `collections`(`id`) ON DELETE CASCADE,
                    CONSTRAINT `fk_labels_documents` FOREIGN KEY (`documentid`) REFERENCES `documents`(`id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_labels_collectionid` ON `labels`(`collectionid`);
                CREATE INDEX IF NOT EXISTS `idx_labels_documentid` ON `labels`(`documentid`);
                CREATE INDEX IF NOT EXISTS `idx_labels_labelvalue` ON `labels`(`labelvalue`(255));
                CREATE INDEX IF NOT EXISTS `idx_labels_collectionid_labelvalue` ON `labels`(`collectionid`, `labelvalue`(255));
                CREATE INDEX IF NOT EXISTS `idx_labels_documentid_labelvalue` ON `labels`(`documentid`, `labelvalue`(255));
                CREATE INDEX IF NOT EXISTS `idx_labels_labelvalue_documentid` ON `labels`(`labelvalue`(255), `documentid`);
                CREATE INDEX IF NOT EXISTS `idx_labels_createdutc` ON `labels`(`createdutc`);

                -- Tags table (unified for collections and documents)
                CREATE TABLE IF NOT EXISTS `tags` (
                    `id` VARCHAR(64) NOT NULL,
                    `collectionid` VARCHAR(64),
                    `documentid` VARCHAR(64),
                    `key` VARCHAR(256) NOT NULL,
                    `value` TEXT,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_tags_collections` FOREIGN KEY (`collectionid`) REFERENCES `collections`(`id`) ON DELETE CASCADE,
                    CONSTRAINT `fk_tags_documents` FOREIGN KEY (`documentid`) REFERENCES `documents`(`id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_tags_collectionid` ON `tags`(`collectionid`);
                CREATE INDEX IF NOT EXISTS `idx_tags_documentid` ON `tags`(`documentid`);
                CREATE INDEX IF NOT EXISTS `idx_tags_key` ON `tags`(`key`);
                CREATE INDEX IF NOT EXISTS `idx_tags_collectionid_key` ON `tags`(`collectionid`, `key`);
                CREATE INDEX IF NOT EXISTS `idx_tags_documentid_key` ON `tags`(`documentid`, `key`);
                CREATE INDEX IF NOT EXISTS `idx_tags_createdutc` ON `tags`(`createdutc`);

                -- Index table mappings
                CREATE TABLE IF NOT EXISTS `indextablemappings` (
                    `id` VARCHAR(64) NOT NULL,
                    `key` VARCHAR(512) NOT NULL,
                    `tablename` VARCHAR(256) NOT NULL,
                    `createdutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    UNIQUE KEY `uk_indextablemappings_key` (`key`(255)),
                    UNIQUE KEY `uk_indextablemappings_tablename` (`tablename`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_indextablemappings_key` ON `indextablemappings`(`key`(255));
                CREATE INDEX IF NOT EXISTS `idx_indextablemappings_tablename` ON `indextablemappings`(`tablename`);
                CREATE INDEX IF NOT EXISTS `idx_indextablemappings_createdutc` ON `indextablemappings`(`createdutc`);

                -- Field constraints table (schema constraints for collections)
                CREATE TABLE IF NOT EXISTS `fieldconstraints` (
                    `id` VARCHAR(64) NOT NULL,
                    `collectionid` VARCHAR(64) NOT NULL,
                    `fieldpath` VARCHAR(512) NOT NULL,
                    `datatype` VARCHAR(64),
                    `required` TINYINT NOT NULL DEFAULT 0,
                    `nullable` TINYINT NOT NULL DEFAULT 1,
                    `regexpattern` VARCHAR(1024),
                    `minvalue` DECIMAL(18,6),
                    `maxvalue` DECIMAL(18,6),
                    `minlength` INT,
                    `maxlength` INT,
                    `allowedvalues` TEXT,
                    `arrayelementtype` VARCHAR(64),
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_fieldconstraints_collections` FOREIGN KEY (`collectionid`) REFERENCES `collections`(`id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_fieldconstraints_collectionid` ON `fieldconstraints`(`collectionid`);
                CREATE UNIQUE INDEX IF NOT EXISTS `idx_fieldconstraints_collectionid_fieldpath` ON `fieldconstraints`(`collectionid`, `fieldpath`(255));

                -- Indexed fields table (selective indexing configuration)
                CREATE TABLE IF NOT EXISTS `indexedfields` (
                    `id` VARCHAR(64) NOT NULL,
                    `collectionid` VARCHAR(64) NOT NULL,
                    `fieldpath` VARCHAR(512) NOT NULL,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_indexedfields_collections` FOREIGN KEY (`collectionid`) REFERENCES `collections`(`id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_indexedfields_collectionid` ON `indexedfields`(`collectionid`);
                CREATE UNIQUE INDEX IF NOT EXISTS `idx_indexedfields_collectionid_fieldpath` ON `indexedfields`(`collectionid`, `fieldpath`(255));

                -- Object locks table (distributed locking for document ingestion)
                CREATE TABLE IF NOT EXISTS `objectlocks` (
                    `id` VARCHAR(64) NOT NULL,
                    `collectionid` VARCHAR(64) NOT NULL,
                    `documentname` VARCHAR(512) NOT NULL,
                    `hostname` VARCHAR(256) NOT NULL,
                    `createdutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_objectlocks_collections` FOREIGN KEY (`collectionid`) REFERENCES `collections`(`id`) ON DELETE CASCADE,
                    CONSTRAINT `uk_objectlocks_collectionid_documentname` UNIQUE (`collectionid`, `documentname`(255))
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_objectlocks_createdutc` ON `objectlocks`(`createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_objectlocks_hostname` ON `objectlocks`(`hostname`);
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
                "ALTER TABLE `documents` ADD COLUMN `contentlength` BIGINT DEFAULT 0;",
                // Add sha256hash column to documents table
                "ALTER TABLE `documents` ADD COLUMN `sha256hash` VARCHAR(128);"
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
                CREATE TABLE IF NOT EXISTS `{tableName}` (
                    `id` VARCHAR(64) NOT NULL,
                    `documentid` VARCHAR(64) NOT NULL,
                    `position` INT,
                    `value` TEXT,
                    `createdutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_{tableName}_documentid` ON `{tableName}`(`documentid`);
                CREATE INDEX IF NOT EXISTS `idx_{tableName}_position` ON `{tableName}`(`position`);
                CREATE INDEX IF NOT EXISTS `idx_{tableName}_createdutc` ON `{tableName}`(`createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_{tableName}_documentid_position` ON `{tableName}`(`documentid`, `position`);
            ";
        }

        /// <summary>
        /// Get the SQL to drop a dynamic index table.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>SQL string.</returns>
        internal static string DropIndexTable(string tableName)
        {
            return $"DROP TABLE IF EXISTS `{tableName}`;";
        }
    }
}
