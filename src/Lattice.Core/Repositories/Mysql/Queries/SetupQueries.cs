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
                    `tenantid` VARCHAR(64),
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

                -- Request history table
                CREATE TABLE IF NOT EXISTS `requesthistory` (
                    `id` VARCHAR(64) NOT NULL,
                    `createdutc` DATETIME(6) NOT NULL,
                    `completedutc` DATETIME(6) NOT NULL,
                    `requesttype` VARCHAR(64) NOT NULL,
                    `method` VARCHAR(16) NOT NULL,
                    `path` TEXT NOT NULL,
                    `url` MEDIUMTEXT NOT NULL,
                    `sourceip` VARCHAR(128) NOT NULL,
                    `collectionid` VARCHAR(64),
                    `documentid` VARCHAR(64),
                    `schemaid` VARCHAR(64),
                    `tablename` VARCHAR(256),
                    `statuscode` INT NOT NULL,
                    `success` TINYINT NOT NULL,
                    `processingtimems` DOUBLE NOT NULL DEFAULT 0,
                    `requestbodylength` BIGINT NOT NULL DEFAULT 0,
                    `responsebodylength` BIGINT NOT NULL DEFAULT 0,
                    `requestbodytruncated` TINYINT NOT NULL DEFAULT 0,
                    `responsebodytruncated` TINYINT NOT NULL DEFAULT 0,
                    `requestcontenttype` VARCHAR(256),
                    `responsecontenttype` VARCHAR(256),
                    `requestheadersjson` MEDIUMTEXT,
                    `requestbody` MEDIUMTEXT,
                    `responseheadersjson` MEDIUMTEXT,
                    `responsebody` MEDIUMTEXT,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_requesthistory_createdutc` ON `requesthistory`(`createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_requesthistory_requesttype_createdutc` ON `requesthistory`(`requesttype`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_requesthistory_method_createdutc` ON `requesthistory`(`method`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_requesthistory_statuscode_createdutc` ON `requesthistory`(`statuscode`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_requesthistory_success_createdutc` ON `requesthistory`(`success`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_requesthistory_collectionid_createdutc` ON `requesthistory`(`collectionid`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_requesthistory_documentid_createdutc` ON `requesthistory`(`documentid`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_requesthistory_schemaid_createdutc` ON `requesthistory`(`schemaid`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_requesthistory_tablename_createdutc` ON `requesthistory`(`tablename`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_requesthistory_sourceip_createdutc` ON `requesthistory`(`sourceip`, `createdutc`);

                -- Tenants table
                CREATE TABLE IF NOT EXISTS `tenants` (
                    `id` VARCHAR(64) NOT NULL,
                    `name` VARCHAR(512) NOT NULL,
                    `region` VARCHAR(256),
                    `active` TINYINT NOT NULL DEFAULT 1,
                    `isprotected` TINYINT NOT NULL DEFAULT 0,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_tenants_name` ON `tenants`(`name`(255));
                CREATE INDEX IF NOT EXISTS `idx_tenants_createdutc` ON `tenants`(`createdutc`);

                -- Users table
                CREATE TABLE IF NOT EXISTS `users` (
                    `id` VARCHAR(64) NOT NULL,
                    `tenantid` VARCHAR(64) NOT NULL,
                    `firstname` VARCHAR(256),
                    `lastname` VARCHAR(256),
                    `email` VARCHAR(256) NOT NULL,
                    `passwordsha256` VARCHAR(128),
                    `isadmin` TINYINT NOT NULL DEFAULT 0,
                    `istenantadmin` TINYINT NOT NULL DEFAULT 0,
                    `active` TINYINT NOT NULL DEFAULT 1,
                    `isprotected` TINYINT NOT NULL DEFAULT 0,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_users_tenants` FOREIGN KEY (`tenantid`) REFERENCES `tenants`(`id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE UNIQUE INDEX IF NOT EXISTS `idx_users_tenantid_email` ON `users`(`tenantid`, `email`);
                CREATE INDEX IF NOT EXISTS `idx_users_tenantid` ON `users`(`tenantid`);

                -- Credentials table (access key used as bearer token)
                CREATE TABLE IF NOT EXISTS `credentials` (
                    `id` VARCHAR(64) NOT NULL,
                    `tenantid` VARCHAR(64) NOT NULL,
                    `userid` VARCHAR(64) NOT NULL,
                    `name` VARCHAR(512),
                    `accesskeysha256` VARCHAR(128) NOT NULL,
                    `accesskeylast4` VARCHAR(16),
                    `expiresutc` DATETIME(6),
                    `lastusedutc` DATETIME(6),
                    `active` TINYINT NOT NULL DEFAULT 1,
                    `isprotected` TINYINT NOT NULL DEFAULT 0,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_credentials_tenants` FOREIGN KEY (`tenantid`) REFERENCES `tenants`(`id`) ON DELETE CASCADE,
                    CONSTRAINT `fk_credentials_users` FOREIGN KEY (`userid`) REFERENCES `users`(`id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE UNIQUE INDEX IF NOT EXISTS `idx_credentials_accesskeysha256` ON `credentials`(`accesskeysha256`);
                CREATE INDEX IF NOT EXISTS `idx_credentials_tenantid` ON `credentials`(`tenantid`);
                CREATE INDEX IF NOT EXISTS `idx_credentials_userid` ON `credentials`(`userid`);

                -- Authentication sessions table (user login sessions)
                CREATE TABLE IF NOT EXISTS `authsessions` (
                    `id` VARCHAR(64) NOT NULL,
                    `tenantid` VARCHAR(64) NOT NULL,
                    `principaltype` INT NOT NULL DEFAULT 0,
                    `userid` VARCHAR(64),
                    `tokenid` VARCHAR(256) NOT NULL,
                    `sourceip` VARCHAR(128),
                    `useragent` VARCHAR(1024),
                    `expiresutc` DATETIME(6) NOT NULL,
                    `lastusedutc` DATETIME(6),
                    `revokedutc` DATETIME(6),
                    `revocationreason` VARCHAR(512),
                    `active` TINYINT NOT NULL DEFAULT 1,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `fk_authsessions_tenants` FOREIGN KEY (`tenantid`) REFERENCES `tenants`(`id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_authsessions_tokenid` ON `authsessions`(`tokenid`);
                CREATE INDEX IF NOT EXISTS `idx_authsessions_tenantid` ON `authsessions`(`tenantid`);
                CREATE INDEX IF NOT EXISTS `idx_authsessions_userid` ON `authsessions`(`userid`);

                -- Roles table (built-in roles have null tenantid)
                CREATE TABLE IF NOT EXISTS `userroles` (
                    `id` VARCHAR(64) NOT NULL,
                    `tenantid` VARCHAR(64),
                    `name` VARCHAR(512) NOT NULL,
                    `isbuiltin` TINYINT NOT NULL DEFAULT 0,
                    `active` TINYINT NOT NULL DEFAULT 1,
                    `isprotected` TINYINT NOT NULL DEFAULT 0,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_userroles_tenantid` ON `userroles`(`tenantid`);
                CREATE INDEX IF NOT EXISTS `idx_userroles_name` ON `userroles`(`name`(255));

                -- Permissions table
                CREATE TABLE IF NOT EXISTS `permissions` (
                    `id` VARCHAR(64) NOT NULL,
                    `tenantid` VARCHAR(64),
                    `name` VARCHAR(512),
                    `resourcetypes` TEXT,
                    `operationtypes` TEXT,
                    `permissiontype` INT NOT NULL DEFAULT 0,
                    `active` TINYINT NOT NULL DEFAULT 1,
                    `isprotected` TINYINT NOT NULL DEFAULT 0,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_permissions_tenantid` ON `permissions`(`tenantid`);

                -- Role/permission maps table
                CREATE TABLE IF NOT EXISTS `rolepermissionmaps` (
                    `id` VARCHAR(64) NOT NULL,
                    `tenantid` VARCHAR(64),
                    `roleid` VARCHAR(64) NOT NULL,
                    `permissionid` VARCHAR(64) NOT NULL,
                    `active` TINYINT NOT NULL DEFAULT 1,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_rolepermissionmaps_roleid` ON `rolepermissionmaps`(`roleid`);
                CREATE INDEX IF NOT EXISTS `idx_rolepermissionmaps_permissionid` ON `rolepermissionmaps`(`permissionid`);

                -- User role assignments table
                CREATE TABLE IF NOT EXISTS `userroleassignments` (
                    `id` VARCHAR(64) NOT NULL,
                    `tenantid` VARCHAR(64) NOT NULL,
                    `userid` VARCHAR(64) NOT NULL,
                    `roleid` VARCHAR(64),
                    `rolename` VARCHAR(512),
                    `resourcescope` INT NOT NULL DEFAULT 0,
                    `resourceid` VARCHAR(64),
                    `inheritstochildren` TINYINT NOT NULL DEFAULT 1,
                    `active` TINYINT NOT NULL DEFAULT 1,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_userroleassignments_tenantid` ON `userroleassignments`(`tenantid`);
                CREATE INDEX IF NOT EXISTS `idx_userroleassignments_userid` ON `userroleassignments`(`userid`);

                -- Credential scope assignments table
                CREATE TABLE IF NOT EXISTS `credentialscopeassignments` (
                    `id` VARCHAR(64) NOT NULL,
                    `tenantid` VARCHAR(64) NOT NULL,
                    `credentialid` VARCHAR(64) NOT NULL,
                    `roleid` VARCHAR(64),
                    `rolename` VARCHAR(512),
                    `resourcescope` INT NOT NULL DEFAULT 0,
                    `resourceid` VARCHAR(64),
                    `permissions` TEXT,
                    `resourcetypes` TEXT,
                    `active` TINYINT NOT NULL DEFAULT 1,
                    `createdutc` DATETIME(6) NOT NULL,
                    `lastupdateutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_credentialscopeassignments_tenantid` ON `credentialscopeassignments`(`tenantid`);
                CREATE INDEX IF NOT EXISTS `idx_credentialscopeassignments_credentialid` ON `credentialscopeassignments`(`credentialid`);

                -- Audit table (append-only security events)
                CREATE TABLE IF NOT EXISTS `audit` (
                    `id` VARCHAR(64) NOT NULL,
                    `tenantid` VARCHAR(64),
                    `eventtype` VARCHAR(128),
                    `requestid` VARCHAR(128),
                    `correlationid` VARCHAR(128),
                    `traceid` VARCHAR(128),
                    `principaltype` INT,
                    `principalid` VARCHAR(64),
                    `userid` VARCHAR(64),
                    `credentialid` VARCHAR(64),
                    `resourcetype` INT,
                    `resourceid` VARCHAR(64),
                    `requesttype` VARCHAR(64),
                    `method` VARCHAR(16),
                    `path` VARCHAR(1024),
                    `sourceip` VARCHAR(128),
                    `authresult` VARCHAR(128),
                    `authzresult` VARCHAR(128),
                    `denialreason` VARCHAR(512),
                    `bypassreason` VARCHAR(512),
                    `requiredpermission` VARCHAR(512),
                    `responsecode` INT NOT NULL DEFAULT 0,
                    `createdutc` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE INDEX IF NOT EXISTS `idx_audit_tenantid_createdutc` ON `audit`(`tenantid`, `createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_audit_createdutc` ON `audit`(`createdutc`);
                CREATE INDEX IF NOT EXISTS `idx_audit_eventtype` ON `audit`(`eventtype`);
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
                "ALTER TABLE `documents` ADD COLUMN `sha256hash` VARCHAR(128);",
                // Add tenantid column then its index (index created here so it runs after the column exists).
                "ALTER TABLE `collections` ADD COLUMN `tenantid` VARCHAR(64);",
                "CREATE INDEX IF NOT EXISTS `idx_collections_tenantid` ON `collections`(`tenantid`);"
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
