namespace Lattice.Core.Repositories.SqlServer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using Lattice.Core.Models;

    /// <summary>
    /// Converts DataRow objects to model objects for SQL Server.
    /// </summary>
    internal static class Converters
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Collection CollectionFromDataRow(DataRow row)
        {
            if (row == null) return null;

            var collection = new Collection
            {
                Id = row["id"]?.ToString(),
                Name = row["name"]?.ToString(),
                Description = row["description"]?.ToString(),
                DocumentsDirectory = row["documentsdirectory"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };

            if (row.Table.Columns.Contains("schemaenforcementmode") && row["schemaenforcementmode"] != DBNull.Value)
            {
                collection.SchemaEnforcementMode = (SchemaEnforcementMode)Convert.ToInt32(row["schemaenforcementmode"]);
            }

            if (row.Table.Columns.Contains("indexingmode") && row["indexingmode"] != DBNull.Value)
            {
                collection.IndexingMode = (IndexingMode)Convert.ToInt32(row["indexingmode"]);
            }

            return collection;
        }

        internal static Document DocumentFromDataRow(DataRow row)
        {
            if (row == null) return null;

            var doc = new Document
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"]?.ToString(),
                SchemaId = row["schemaid"]?.ToString(),
                Name = row["name"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };

            if (row.Table.Columns.Contains("contentlength") && row["contentlength"] != DBNull.Value)
            {
                doc.ContentLength = Convert.ToInt64(row["contentlength"]);
            }

            if (row.Table.Columns.Contains("sha256hash") && row["sha256hash"] != DBNull.Value)
            {
                doc.Sha256Hash = row["sha256hash"]?.ToString();
            }

            return doc;
        }

        internal static Schema SchemaFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Schema
            {
                Id = row["id"]?.ToString(),
                Name = row["name"]?.ToString(),
                Hash = row["hash"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static SchemaElement SchemaElementFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new SchemaElement
            {
                Id = row["id"]?.ToString(),
                SchemaId = row["schemaid"]?.ToString(),
                Position = Convert.ToInt32(row["position"]),
                Key = row["key"]?.ToString(),
                DataType = row["datatype"]?.ToString(),
                Nullable = Convert.ToBoolean(row["nullable"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static Label LabelFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Label
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"] != DBNull.Value ? row["collectionid"]?.ToString() : null,
                DocumentId = row["documentid"] != DBNull.Value ? row["documentid"]?.ToString() : null,
                LabelValue = row["labelvalue"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static Tag TagFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Tag
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"] != DBNull.Value ? row["collectionid"]?.ToString() : null,
                DocumentId = row["documentid"] != DBNull.Value ? row["documentid"]?.ToString() : null,
                Key = row["key"]?.ToString(),
                Value = row["value"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static IndexTableMapping IndexTableMappingFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new IndexTableMapping
            {
                Id = row["id"]?.ToString(),
                Key = row["key"]?.ToString(),
                TableName = row["tablename"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"])
            };
        }

        internal static DocumentValue DocumentValueFromDataRow(DataRow row)
        {
            if (row == null) return null;

            int? position = null;
            if (row["position"] != DBNull.Value)
                position = Convert.ToInt32(row["position"]);

            return new DocumentValue
            {
                Id = row["id"]?.ToString(),
                DocumentId = row["documentid"]?.ToString(),
                Position = position,
                Value = row["value"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"])
            };
        }

        internal static string ToTimestamp(DateTime dt)
        {
            return dt.ToString(TimestampFormat);
        }

        internal static FieldConstraint FieldConstraintFromDataRow(DataRow row)
        {
            if (row == null) return null;

            var constraint = new FieldConstraint
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"]?.ToString(),
                FieldPath = row["fieldpath"]?.ToString(),
                DataType = row["datatype"] != DBNull.Value ? row["datatype"]?.ToString() : null,
                Required = row["required"] != DBNull.Value && Convert.ToBoolean(row["required"]),
                Nullable = row["nullable"] == DBNull.Value || Convert.ToBoolean(row["nullable"]),
                RegexPattern = row["regexpattern"] != DBNull.Value ? row["regexpattern"]?.ToString() : null,
                ArrayElementType = row["arrayelementtype"] != DBNull.Value ? row["arrayelementtype"]?.ToString() : null,
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };

            if (row["minvalue"] != DBNull.Value)
                constraint.MinValue = Convert.ToDecimal(row["minvalue"]);

            if (row["maxvalue"] != DBNull.Value)
                constraint.MaxValue = Convert.ToDecimal(row["maxvalue"]);

            if (row["minlength"] != DBNull.Value)
                constraint.MinLength = Convert.ToInt32(row["minlength"]);

            if (row["maxlength"] != DBNull.Value)
                constraint.MaxLength = Convert.ToInt32(row["maxlength"]);

            if (row["allowedvalues"] != DBNull.Value)
            {
                string allowedValuesJson = row["allowedvalues"]?.ToString();
                if (!string.IsNullOrWhiteSpace(allowedValuesJson))
                {
                    try
                    {
                        constraint.AllowedValues = JsonSerializer.Deserialize<List<string>>(allowedValuesJson);
                    }
                    catch
                    {
                        constraint.AllowedValues = new List<string>();
                    }
                }
            }

            return constraint;
        }

        internal static IndexedField IndexedFieldFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new IndexedField
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"]?.ToString(),
                FieldPath = row["fieldpath"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static ObjectLock ObjectLockFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new ObjectLock
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"]?.ToString(),
                DocumentName = row["documentname"]?.ToString(),
                Hostname = row["hostname"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"])
            };
        }

        internal static IndexTableEntry IndexTableEntryFromDataRow(DataRow row)
        {
            if (row == null) return null;

            int? position = null;
            if (row["position"] != DBNull.Value)
                position = Convert.ToInt32(row["position"]);

            return new IndexTableEntry
            {
                Id = row["id"]?.ToString(),
                DocumentId = row["documentid"]?.ToString(),
                Position = position,
                Value = row["value"] != DBNull.Value ? row["value"]?.ToString() : null,
                CreatedUtc = Convert.ToDateTime(row["createdutc"])
            };
        }

        internal static RequestHistoryEntry RequestHistoryEntryFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new RequestHistoryEntry
            {
                Id = row["id"]?.ToString(),
                CreatedUtc = DateTime.SpecifyKind(Convert.ToDateTime(row["createdutc"]), DateTimeKind.Utc),
                CompletedUtc = DateTime.SpecifyKind(Convert.ToDateTime(row["completedutc"]), DateTimeKind.Utc),
                RequestType = row["requesttype"]?.ToString() ?? "unknown",
                Method = row["method"]?.ToString() ?? "GET",
                Path = row["path"]?.ToString() ?? "/",
                Url = row["url"]?.ToString() ?? "/",
                SourceIp = row["sourceip"]?.ToString() ?? "unknown",
                CollectionId = row["collectionid"] != DBNull.Value ? row["collectionid"]?.ToString() : null,
                DocumentId = row["documentid"] != DBNull.Value ? row["documentid"]?.ToString() : null,
                SchemaId = row["schemaid"] != DBNull.Value ? row["schemaid"]?.ToString() : null,
                TableName = row["tablename"] != DBNull.Value ? row["tablename"]?.ToString() : null,
                StatusCode = Convert.ToInt32(row["statuscode"]),
                Success = Convert.ToBoolean(row["success"]),
                ProcessingTimeMs = Convert.ToDouble(row["processingtimems"]),
                RequestBodyLength = Convert.ToInt64(row["requestbodylength"]),
                ResponseBodyLength = Convert.ToInt64(row["responsebodylength"]),
                RequestBodyTruncated = Convert.ToBoolean(row["requestbodytruncated"]),
                ResponseBodyTruncated = Convert.ToBoolean(row["responsebodytruncated"]),
                RequestContentType = row["requestcontenttype"] != DBNull.Value ? row["requestcontenttype"]?.ToString() : null,
                ResponseContentType = row["responsecontenttype"] != DBNull.Value ? row["responsecontenttype"]?.ToString() : null
            };
        }

        internal static RequestHistoryDetail RequestHistoryDetailFromDataRow(DataRow row)
        {
            RequestHistoryEntry entry = RequestHistoryEntryFromDataRow(row);
            if (entry == null) return null;

            return new RequestHistoryDetail
            {
                Id = entry.Id,
                CreatedUtc = entry.CreatedUtc,
                CompletedUtc = entry.CompletedUtc,
                RequestType = entry.RequestType,
                Method = entry.Method,
                Path = entry.Path,
                Url = entry.Url,
                SourceIp = entry.SourceIp,
                CollectionId = entry.CollectionId,
                DocumentId = entry.DocumentId,
                SchemaId = entry.SchemaId,
                TableName = entry.TableName,
                StatusCode = entry.StatusCode,
                Success = entry.Success,
                ProcessingTimeMs = entry.ProcessingTimeMs,
                RequestBodyLength = entry.RequestBodyLength,
                ResponseBodyLength = entry.ResponseBodyLength,
                RequestBodyTruncated = entry.RequestBodyTruncated,
                ResponseBodyTruncated = entry.ResponseBodyTruncated,
                RequestContentType = entry.RequestContentType,
                ResponseContentType = entry.ResponseContentType,
                RequestHeaders = DeserializeStringDictionary(row, "requestheadersjson"),
                RequestBody = row.Table.Columns.Contains("requestbody") && row["requestbody"] != DBNull.Value ? row["requestbody"]?.ToString() : null,
                ResponseHeaders = DeserializeStringDictionary(row, "responseheadersjson"),
                ResponseBody = row.Table.Columns.Contains("responsebody") && row["responsebody"] != DBNull.Value ? row["responsebody"]?.ToString() : null
            };
        }

        private static Dictionary<string, string> DeserializeStringDictionary(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            string json = row[columnName]?.ToString();
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        internal static Tenant TenantFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Tenant
            {
                Id = row["id"]?.ToString(),
                Name = row["name"]?.ToString(),
                Region = row["region"] != DBNull.Value ? row["region"]?.ToString() : null,
                Active = Convert.ToBoolean(row["active"]),
                IsProtected = Convert.ToBoolean(row["isprotected"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static User UserFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new User
            {
                Id = row["id"]?.ToString(),
                TenantId = row["tenantid"]?.ToString(),
                FirstName = row["firstname"] != DBNull.Value ? row["firstname"]?.ToString() : null,
                LastName = row["lastname"] != DBNull.Value ? row["lastname"]?.ToString() : null,
                Email = row["email"]?.ToString(),
                PasswordSha256 = row["passwordsha256"] != DBNull.Value ? row["passwordsha256"]?.ToString() : null,
                IsAdmin = Convert.ToBoolean(row["isadmin"]),
                IsTenantAdmin = Convert.ToBoolean(row["istenantadmin"]),
                Active = Convert.ToBoolean(row["active"]),
                IsProtected = Convert.ToBoolean(row["isprotected"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static Credential CredentialFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Credential
            {
                Id = row["id"]?.ToString(),
                TenantId = row["tenantid"]?.ToString(),
                UserId = row["userid"]?.ToString(),
                Name = row["name"] != DBNull.Value ? row["name"]?.ToString() : null,
                AccessKeySha256 = row["accesskeysha256"]?.ToString(),
                AccessKeyLast4 = row["accesskeylast4"] != DBNull.Value ? row["accesskeylast4"]?.ToString() : null,
                ExpiresUtc = row["expiresutc"] != DBNull.Value ? Convert.ToDateTime(row["expiresutc"]) : (DateTime?)null,
                LastUsedUtc = row["lastusedutc"] != DBNull.Value ? Convert.ToDateTime(row["lastusedutc"]) : (DateTime?)null,
                Active = Convert.ToBoolean(row["active"]),
                IsProtected = Convert.ToBoolean(row["isprotected"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static AuthSession AuthSessionFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new AuthSession
            {
                Id = row["id"]?.ToString(),
                TenantId = row["tenantid"]?.ToString(),
                PrincipalType = (PrincipalType)Convert.ToInt32(row["principaltype"]),
                UserId = row["userid"] != DBNull.Value ? row["userid"]?.ToString() : null,
                TokenId = row["tokenid"]?.ToString(),
                SourceIp = row["sourceip"] != DBNull.Value ? row["sourceip"]?.ToString() : null,
                UserAgent = row["useragent"] != DBNull.Value ? row["useragent"]?.ToString() : null,
                ExpiresUtc = Convert.ToDateTime(row["expiresutc"]),
                LastUsedUtc = row["lastusedutc"] != DBNull.Value ? Convert.ToDateTime(row["lastusedutc"]) : (DateTime?)null,
                RevokedUtc = row["revokedutc"] != DBNull.Value ? Convert.ToDateTime(row["revokedutc"]) : (DateTime?)null,
                RevocationReason = row["revocationreason"] != DBNull.Value ? row["revocationreason"]?.ToString() : null,
                Active = Convert.ToBoolean(row["active"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static UserRole UserRoleFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new UserRole
            {
                Id = row["id"]?.ToString(),
                TenantId = row["tenantid"] != DBNull.Value ? row["tenantid"]?.ToString() : null,
                Name = row["name"]?.ToString(),
                IsBuiltIn = Convert.ToBoolean(row["isbuiltin"]),
                Active = Convert.ToBoolean(row["active"]),
                IsProtected = Convert.ToBoolean(row["isprotected"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static Permission PermissionFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Permission
            {
                Id = row["id"]?.ToString(),
                TenantId = row["tenantid"] != DBNull.Value ? row["tenantid"]?.ToString() : null,
                Name = row["name"] != DBNull.Value ? row["name"]?.ToString() : null,
                ResourceTypes = DeserializeEnumList<ResourceType>(row, "resourcetypes"),
                OperationTypes = DeserializeEnumList<OperationType>(row, "operationtypes"),
                PermissionType = (PermissionType)Convert.ToInt32(row["permissiontype"]),
                Active = Convert.ToBoolean(row["active"]),
                IsProtected = Convert.ToBoolean(row["isprotected"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static RolePermissionMap RolePermissionMapFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new RolePermissionMap
            {
                Id = row["id"]?.ToString(),
                TenantId = row["tenantid"] != DBNull.Value ? row["tenantid"]?.ToString() : null,
                RoleId = row["roleid"]?.ToString(),
                PermissionId = row["permissionid"]?.ToString(),
                Active = Convert.ToBoolean(row["active"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static UserRoleAssignment UserRoleAssignmentFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new UserRoleAssignment
            {
                Id = row["id"]?.ToString(),
                TenantId = row["tenantid"]?.ToString(),
                UserId = row["userid"]?.ToString(),
                RoleId = row["roleid"] != DBNull.Value ? row["roleid"]?.ToString() : null,
                RoleName = row["rolename"] != DBNull.Value ? row["rolename"]?.ToString() : null,
                ResourceScope = (ResourceScope)Convert.ToInt32(row["resourcescope"]),
                ResourceId = row["resourceid"] != DBNull.Value ? row["resourceid"]?.ToString() : null,
                InheritsToChildren = Convert.ToBoolean(row["inheritstochildren"]),
                Active = Convert.ToBoolean(row["active"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static CredentialScopeAssignment CredentialScopeAssignmentFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new CredentialScopeAssignment
            {
                Id = row["id"]?.ToString(),
                TenantId = row["tenantid"]?.ToString(),
                CredentialId = row["credentialid"]?.ToString(),
                RoleId = row["roleid"] != DBNull.Value ? row["roleid"]?.ToString() : null,
                RoleName = row["rolename"] != DBNull.Value ? row["rolename"]?.ToString() : null,
                ResourceScope = (ResourceScope)Convert.ToInt32(row["resourcescope"]),
                ResourceId = row["resourceid"] != DBNull.Value ? row["resourceid"]?.ToString() : null,
                Permissions = DeserializeEnumList<OperationType>(row, "permissions"),
                ResourceTypes = DeserializeEnumList<ResourceType>(row, "resourcetypes"),
                Active = Convert.ToBoolean(row["active"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static AuditEntry AuditEntryFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new AuditEntry
            {
                Id = row["id"]?.ToString(),
                TenantId = row["tenantid"] != DBNull.Value ? row["tenantid"]?.ToString() : null,
                EventType = row["eventtype"] != DBNull.Value ? row["eventtype"]?.ToString() : null,
                RequestId = row["requestid"] != DBNull.Value ? row["requestid"]?.ToString() : null,
                CorrelationId = row["correlationid"] != DBNull.Value ? row["correlationid"]?.ToString() : null,
                TraceId = row["traceid"] != DBNull.Value ? row["traceid"]?.ToString() : null,
                PrincipalType = row["principaltype"] != DBNull.Value ? (PrincipalType)Convert.ToInt32(row["principaltype"]) : (PrincipalType?)null,
                PrincipalId = row["principalid"] != DBNull.Value ? row["principalid"]?.ToString() : null,
                UserId = row["userid"] != DBNull.Value ? row["userid"]?.ToString() : null,
                CredentialId = row["credentialid"] != DBNull.Value ? row["credentialid"]?.ToString() : null,
                ResourceType = row["resourcetype"] != DBNull.Value ? (ResourceType)Convert.ToInt32(row["resourcetype"]) : (ResourceType?)null,
                ResourceId = row["resourceid"] != DBNull.Value ? row["resourceid"]?.ToString() : null,
                RequestType = row["requesttype"] != DBNull.Value ? row["requesttype"]?.ToString() : null,
                Method = row["method"] != DBNull.Value ? row["method"]?.ToString() : null,
                Path = row["path"] != DBNull.Value ? row["path"]?.ToString() : null,
                SourceIp = row["sourceip"] != DBNull.Value ? row["sourceip"]?.ToString() : null,
                AuthResult = row["authresult"] != DBNull.Value ? row["authresult"]?.ToString() : null,
                AuthzResult = row["authzresult"] != DBNull.Value ? row["authzresult"]?.ToString() : null,
                DenialReason = row["denialreason"] != DBNull.Value ? row["denialreason"]?.ToString() : null,
                BypassReason = row["bypassreason"] != DBNull.Value ? row["bypassreason"]?.ToString() : null,
                RequiredPermission = row["requiredpermission"] != DBNull.Value ? row["requiredpermission"]?.ToString() : null,
                ResponseCode = Convert.ToInt32(row["responsecode"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"])
            };
        }

        private static List<TEnum> DeserializeEnumList<TEnum>(DataRow row, string columnName) where TEnum : struct, Enum
        {
            List<TEnum> result = new List<TEnum>();

            if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                return result;

            string json = row[columnName]?.ToString();
            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                List<string> names = JsonSerializer.Deserialize<List<string>>(json);
                if (names != null)
                {
                    foreach (string name in names)
                    {
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        if (Enum.TryParse<TEnum>(name, out TEnum value)) result.Add(value);
                    }
                }
            }
            catch
            {
                return new List<TEnum>();
            }

            return result;
        }
    }
}
