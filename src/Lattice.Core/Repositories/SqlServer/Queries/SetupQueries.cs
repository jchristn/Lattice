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
                    [tenantid] NVARCHAR(64),
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

                -- Request history table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'requesthistory')
                CREATE TABLE [requesthistory] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [createdutc] DATETIME2 NOT NULL,
                    [completedutc] DATETIME2 NOT NULL,
                    [requesttype] NVARCHAR(64) NOT NULL,
                    [method] NVARCHAR(16) NOT NULL,
                    [path] NVARCHAR(MAX) NOT NULL,
                    [url] NVARCHAR(MAX) NOT NULL,
                    [sourceip] NVARCHAR(128) NOT NULL,
                    [collectionid] NVARCHAR(64),
                    [documentid] NVARCHAR(64),
                    [schemaid] NVARCHAR(64),
                    [tablename] NVARCHAR(256),
                    [statuscode] INT NOT NULL,
                    [success] BIT NOT NULL,
                    [processingtimems] FLOAT NOT NULL DEFAULT 0,
                    [requestbodylength] BIGINT NOT NULL DEFAULT 0,
                    [responsebodylength] BIGINT NOT NULL DEFAULT 0,
                    [requestbodytruncated] BIT NOT NULL DEFAULT 0,
                    [responsebodytruncated] BIT NOT NULL DEFAULT 0,
                    [requestcontenttype] NVARCHAR(256),
                    [responsecontenttype] NVARCHAR(256),
                    [requestheadersjson] NVARCHAR(MAX),
                    [requestbody] NVARCHAR(MAX),
                    [responseheadersjson] NVARCHAR(MAX),
                    [responsebody] NVARCHAR(MAX)
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_createdutc')
                CREATE INDEX [idx_requesthistory_createdutc] ON [requesthistory]([createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_requesttype_createdutc')
                CREATE INDEX [idx_requesthistory_requesttype_createdutc] ON [requesthistory]([requesttype], [createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_method_createdutc')
                CREATE INDEX [idx_requesthistory_method_createdutc] ON [requesthistory]([method], [createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_statuscode_createdutc')
                CREATE INDEX [idx_requesthistory_statuscode_createdutc] ON [requesthistory]([statuscode], [createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_success_createdutc')
                CREATE INDEX [idx_requesthistory_success_createdutc] ON [requesthistory]([success], [createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_collectionid_createdutc')
                CREATE INDEX [idx_requesthistory_collectionid_createdutc] ON [requesthistory]([collectionid], [createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_documentid_createdutc')
                CREATE INDEX [idx_requesthistory_documentid_createdutc] ON [requesthistory]([documentid], [createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_schemaid_createdutc')
                CREATE INDEX [idx_requesthistory_schemaid_createdutc] ON [requesthistory]([schemaid], [createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_tablename_createdutc')
                CREATE INDEX [idx_requesthistory_tablename_createdutc] ON [requesthistory]([tablename], [createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_requesthistory_sourceip_createdutc')
                CREATE INDEX [idx_requesthistory_sourceip_createdutc] ON [requesthistory]([sourceip], [createdutc]);

                -- Tenants table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tenants')
                CREATE TABLE [tenants] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [name] NVARCHAR(512) NOT NULL,
                    [region] NVARCHAR(256),
                    [active] BIT NOT NULL DEFAULT 1,
                    [isprotected] BIT NOT NULL DEFAULT 0,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_tenants_name')
                CREATE INDEX [idx_tenants_name] ON [tenants]([name]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_tenants_createdutc')
                CREATE INDEX [idx_tenants_createdutc] ON [tenants]([createdutc]);

                -- Users table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
                CREATE TABLE [users] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [tenantid] NVARCHAR(64) NOT NULL,
                    [firstname] NVARCHAR(256),
                    [lastname] NVARCHAR(256),
                    [email] NVARCHAR(512) NOT NULL,
                    [passwordsha256] NVARCHAR(128),
                    [isadmin] BIT NOT NULL DEFAULT 0,
                    [istenantadmin] BIT NOT NULL DEFAULT 0,
                    [active] BIT NOT NULL DEFAULT 1,
                    [isprotected] BIT NOT NULL DEFAULT 0,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_users_tenants] FOREIGN KEY ([tenantid]) REFERENCES [tenants]([id]) ON DELETE CASCADE
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_users_tenantid_email')
                CREATE UNIQUE INDEX [idx_users_tenantid_email] ON [users]([tenantid], [email]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_users_tenantid')
                CREATE INDEX [idx_users_tenantid] ON [users]([tenantid]);

                -- Credentials table (access key used as bearer token)
                -- Note: the tenant FK intentionally does not cascade because the userid FK
                -- already provides a cascade path to tenants (via users); SQL Server forbids
                -- multiple cascade paths to the same table.
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'credentials')
                CREATE TABLE [credentials] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [tenantid] NVARCHAR(64) NOT NULL,
                    [userid] NVARCHAR(64) NOT NULL,
                    [name] NVARCHAR(512),
                    [accesskey] NVARCHAR(128),
                    [accesskeysha256] NVARCHAR(128) NOT NULL,
                    [accesskeylast4] NVARCHAR(16),
                    [expiresutc] DATETIME2,
                    [lastusedutc] DATETIME2,
                    [active] BIT NOT NULL DEFAULT 1,
                    [isprotected] BIT NOT NULL DEFAULT 0,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_credentials_tenants] FOREIGN KEY ([tenantid]) REFERENCES [tenants]([id]),
                    CONSTRAINT [fk_credentials_users] FOREIGN KEY ([userid]) REFERENCES [users]([id]) ON DELETE CASCADE
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentials_accesskeysha256')
                CREATE UNIQUE INDEX [idx_credentials_accesskeysha256] ON [credentials]([accesskeysha256]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentials_tenantid')
                CREATE INDEX [idx_credentials_tenantid] ON [credentials]([tenantid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentials_userid')
                CREATE INDEX [idx_credentials_userid] ON [credentials]([userid]);

                -- Authentication sessions table (user login sessions)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'authsessions')
                CREATE TABLE [authsessions] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [tenantid] NVARCHAR(64) NOT NULL,
                    [principaltype] INT NOT NULL DEFAULT 0,
                    [userid] NVARCHAR(64),
                    [tokenid] NVARCHAR(128) NOT NULL,
                    [sourceip] NVARCHAR(128),
                    [useragent] NVARCHAR(1024),
                    [expiresutc] DATETIME2 NOT NULL,
                    [lastusedutc] DATETIME2,
                    [revokedutc] DATETIME2,
                    [revocationreason] NVARCHAR(512),
                    [active] BIT NOT NULL DEFAULT 1,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL,
                    CONSTRAINT [fk_authsessions_tenants] FOREIGN KEY ([tenantid]) REFERENCES [tenants]([id]) ON DELETE CASCADE
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_authsessions_tokenid')
                CREATE INDEX [idx_authsessions_tokenid] ON [authsessions]([tokenid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_authsessions_tenantid')
                CREATE INDEX [idx_authsessions_tenantid] ON [authsessions]([tenantid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_authsessions_userid')
                CREATE INDEX [idx_authsessions_userid] ON [authsessions]([userid]);

                -- Roles table (built-in roles have null tenantid)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'userroles')
                CREATE TABLE [userroles] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [tenantid] NVARCHAR(64),
                    [name] NVARCHAR(512) NOT NULL,
                    [isbuiltin] BIT NOT NULL DEFAULT 0,
                    [active] BIT NOT NULL DEFAULT 1,
                    [isprotected] BIT NOT NULL DEFAULT 0,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_userroles_tenantid')
                CREATE INDEX [idx_userroles_tenantid] ON [userroles]([tenantid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_userroles_name')
                CREATE INDEX [idx_userroles_name] ON [userroles]([name]);

                -- Permissions table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'permissions')
                CREATE TABLE [permissions] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [tenantid] NVARCHAR(64),
                    [name] NVARCHAR(512),
                    [resourcetypes] NVARCHAR(MAX),
                    [operationtypes] NVARCHAR(MAX),
                    [permissiontype] INT NOT NULL DEFAULT 0,
                    [active] BIT NOT NULL DEFAULT 1,
                    [isprotected] BIT NOT NULL DEFAULT 0,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_permissions_tenantid')
                CREATE INDEX [idx_permissions_tenantid] ON [permissions]([tenantid]);

                -- Role/permission maps table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'rolepermissionmaps')
                CREATE TABLE [rolepermissionmaps] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [tenantid] NVARCHAR(64),
                    [roleid] NVARCHAR(64) NOT NULL,
                    [permissionid] NVARCHAR(64) NOT NULL,
                    [active] BIT NOT NULL DEFAULT 1,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_rolepermissionmaps_roleid')
                CREATE INDEX [idx_rolepermissionmaps_roleid] ON [rolepermissionmaps]([roleid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_rolepermissionmaps_permissionid')
                CREATE INDEX [idx_rolepermissionmaps_permissionid] ON [rolepermissionmaps]([permissionid]);

                -- User role assignments table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'userroleassignments')
                CREATE TABLE [userroleassignments] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [tenantid] NVARCHAR(64) NOT NULL,
                    [userid] NVARCHAR(64) NOT NULL,
                    [roleid] NVARCHAR(64),
                    [rolename] NVARCHAR(512),
                    [resourcescope] INT NOT NULL DEFAULT 0,
                    [resourceid] NVARCHAR(64),
                    [inheritstochildren] BIT NOT NULL DEFAULT 1,
                    [active] BIT NOT NULL DEFAULT 1,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_userroleassignments_tenantid')
                CREATE INDEX [idx_userroleassignments_tenantid] ON [userroleassignments]([tenantid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_userroleassignments_userid')
                CREATE INDEX [idx_userroleassignments_userid] ON [userroleassignments]([userid]);

                -- Credential scope assignments table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'credentialscopeassignments')
                CREATE TABLE [credentialscopeassignments] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [tenantid] NVARCHAR(64) NOT NULL,
                    [credentialid] NVARCHAR(64) NOT NULL,
                    [roleid] NVARCHAR(64),
                    [rolename] NVARCHAR(512),
                    [resourcescope] INT NOT NULL DEFAULT 0,
                    [resourceid] NVARCHAR(64),
                    [permissions] NVARCHAR(MAX),
                    [resourcetypes] NVARCHAR(MAX),
                    [active] BIT NOT NULL DEFAULT 1,
                    [createdutc] DATETIME2 NOT NULL,
                    [lastupdateutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentialscopeassignments_tenantid')
                CREATE INDEX [idx_credentialscopeassignments_tenantid] ON [credentialscopeassignments]([tenantid]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentialscopeassignments_credentialid')
                CREATE INDEX [idx_credentialscopeassignments_credentialid] ON [credentialscopeassignments]([credentialid]);

                -- Audit table (append-only security events)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'audit')
                CREATE TABLE [audit] (
                    [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                    [tenantid] NVARCHAR(64),
                    [eventtype] NVARCHAR(128),
                    [requestid] NVARCHAR(128),
                    [correlationid] NVARCHAR(128),
                    [traceid] NVARCHAR(128),
                    [principaltype] INT,
                    [principalid] NVARCHAR(64),
                    [userid] NVARCHAR(64),
                    [credentialid] NVARCHAR(64),
                    [resourcetype] INT,
                    [resourceid] NVARCHAR(64),
                    [requesttype] NVARCHAR(64),
                    [method] NVARCHAR(16),
                    [path] NVARCHAR(MAX),
                    [sourceip] NVARCHAR(128),
                    [authresult] NVARCHAR(128),
                    [authzresult] NVARCHAR(128),
                    [denialreason] NVARCHAR(512),
                    [bypassreason] NVARCHAR(512),
                    [requiredpermission] NVARCHAR(256),
                    [responsecode] INT NOT NULL DEFAULT 0,
                    [createdutc] DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_audit_tenantid_createdutc')
                CREATE INDEX [idx_audit_tenantid_createdutc] ON [audit]([tenantid], [createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_audit_createdutc')
                CREATE INDEX [idx_audit_createdutc] ON [audit]([createdutc]);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_audit_eventtype')
                CREATE INDEX [idx_audit_eventtype] ON [audit]([eventtype]);
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
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('documents') AND name = 'sha256hash') ALTER TABLE [documents] ADD [sha256hash] NVARCHAR(128);",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('collections') AND name = 'tenantid') ALTER TABLE [collections] ADD [tenantid] NVARCHAR(64);",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('credentials') AND name = 'accesskey') ALTER TABLE [credentials] ADD [accesskey] NVARCHAR(128);",
                "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_collections_tenantid') CREATE INDEX [idx_collections_tenantid] ON [collections]([tenantid]);"
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
