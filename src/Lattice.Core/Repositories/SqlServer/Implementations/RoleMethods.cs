namespace Lattice.Core.Repositories.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// SQL Server implementation of role, permission, and assignment methods.
    /// </summary>
    internal class RoleMethods : IRoleMethods
    {
        private readonly SqlServerRepository _Repo;

        internal RoleMethods(SqlServerRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #region Roles

        public async Task<UserRole> CreateRole(UserRole role, CancellationToken token = default)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO [userroles] ([id], [tenantid], [name], [isbuiltin], [active], [isprotected], [createdutc], [lastupdateutc])
                OUTPUT INSERTED.*
                VALUES ('{Sanitizer.Sanitize(role.Id)}',
                        {(role.TenantId != null ? $"'{Sanitizer.Sanitize(role.TenantId)}'" : "NULL")},
                        N'{Sanitizer.Sanitize(role.Name)}',
                        {(role.IsBuiltIn ? 1 : 0)},
                        {(role.Active ? 1 : 0)},
                        {(role.IsProtected ? 1 : 0)},
                        '{Converters.ToTimestamp(role.CreatedUtc)}',
                        '{Converters.ToTimestamp(role.LastUpdateUtc)}');
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.UserRoleFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<UserRole> ReadRoleById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [userroles] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.UserRoleFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<UserRole> ReadRoleByName(string tenantId, string name, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            token.ThrowIfCancellationRequested();

            string tenantClause = tenantId != null
                ? $"([tenantid] = '{Sanitizer.Sanitize(tenantId)}' OR [tenantid] IS NULL)"
                : "[tenantid] IS NULL";

            string query = $"SELECT TOP 1 * FROM [userroles] WHERE {tenantClause} AND [name] = N'{Sanitizer.Sanitize(name)}' ORDER BY [tenantid] DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.UserRoleFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<UserRole>> ReadRoles(string tenantId, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            string tenantClause = tenantId != null
                ? $"([tenantid] = '{Sanitizer.Sanitize(tenantId)}' OR [tenantid] IS NULL)"
                : "[tenantid] IS NULL";

            string query = $"SELECT * FROM [userroles] WHERE {tenantClause} ORDER BY [createdutc] DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<UserRole> roles = new List<UserRole>();
            foreach (DataRow row in result.Rows)
                roles.Add(Converters.UserRoleFromDataRow(row));

            return roles;
        }

        public async Task<UserRole> UpdateRole(UserRole role, CancellationToken token = default)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            token.ThrowIfCancellationRequested();

            role.LastUpdateUtc = DateTime.UtcNow;

            string query = $@"
                UPDATE [userroles] SET
                    [name] = N'{Sanitizer.Sanitize(role.Name)}',
                    [isbuiltin] = {(role.IsBuiltIn ? 1 : 0)},
                    [active] = {(role.Active ? 1 : 0)},
                    [isprotected] = {(role.IsProtected ? 1 : 0)},
                    [lastupdateutc] = '{Converters.ToTimestamp(role.LastUpdateUtc)}'
                OUTPUT INSERTED.*
                WHERE [id] = '{Sanitizer.Sanitize(role.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.UserRoleFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task DeleteRole(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM [userroles] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        #endregion

        #region Permissions

        public async Task<Permission> CreatePermission(Permission permission, CancellationToken token = default)
        {
            if (permission == null) throw new ArgumentNullException(nameof(permission));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO [permissions] ([id], [tenantid], [name], [resourcetypes], [operationtypes], [permissiontype], [active], [isprotected], [createdutc], [lastupdateutc])
                OUTPUT INSERTED.*
                VALUES ('{Sanitizer.Sanitize(permission.Id)}',
                        {(permission.TenantId != null ? $"'{Sanitizer.Sanitize(permission.TenantId)}'" : "NULL")},
                        {(permission.Name != null ? $"N'{Sanitizer.Sanitize(permission.Name)}'" : "NULL")},
                        N'{Sanitizer.Sanitize(SerializeEnumNames(permission.ResourceTypes))}',
                        N'{Sanitizer.Sanitize(SerializeEnumNames(permission.OperationTypes))}',
                        {(int)permission.PermissionType},
                        {(permission.Active ? 1 : 0)},
                        {(permission.IsProtected ? 1 : 0)},
                        '{Converters.ToTimestamp(permission.CreatedUtc)}',
                        '{Converters.ToTimestamp(permission.LastUpdateUtc)}');
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.PermissionFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Permission> ReadPermissionById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [permissions] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.PermissionFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<Permission>> ReadPermissions(string tenantId, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            string tenantClause = tenantId != null
                ? $"([tenantid] = '{Sanitizer.Sanitize(tenantId)}' OR [tenantid] IS NULL)"
                : "[tenantid] IS NULL";

            string query = $"SELECT * FROM [permissions] WHERE {tenantClause} ORDER BY [createdutc] DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<Permission> permissions = new List<Permission>();
            foreach (DataRow row in result.Rows)
                permissions.Add(Converters.PermissionFromDataRow(row));

            return permissions;
        }

        public async Task<Permission> UpdatePermission(Permission permission, CancellationToken token = default)
        {
            if (permission == null) throw new ArgumentNullException(nameof(permission));
            token.ThrowIfCancellationRequested();

            permission.LastUpdateUtc = DateTime.UtcNow;

            string query = $@"
                UPDATE [permissions] SET
                    [name] = {(permission.Name != null ? $"N'{Sanitizer.Sanitize(permission.Name)}'" : "NULL")},
                    [resourcetypes] = N'{Sanitizer.Sanitize(SerializeEnumNames(permission.ResourceTypes))}',
                    [operationtypes] = N'{Sanitizer.Sanitize(SerializeEnumNames(permission.OperationTypes))}',
                    [permissiontype] = {(int)permission.PermissionType},
                    [active] = {(permission.Active ? 1 : 0)},
                    [isprotected] = {(permission.IsProtected ? 1 : 0)},
                    [lastupdateutc] = '{Converters.ToTimestamp(permission.LastUpdateUtc)}'
                OUTPUT INSERTED.*
                WHERE [id] = '{Sanitizer.Sanitize(permission.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.PermissionFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task DeletePermission(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM [permissions] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        #endregion

        #region Role-Permission-Maps

        public async Task<RolePermissionMap> CreateRolePermissionMap(RolePermissionMap map, CancellationToken token = default)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO [rolepermissionmaps] ([id], [tenantid], [roleid], [permissionid], [active], [createdutc], [lastupdateutc])
                OUTPUT INSERTED.*
                VALUES ('{Sanitizer.Sanitize(map.Id)}',
                        {(map.TenantId != null ? $"'{Sanitizer.Sanitize(map.TenantId)}'" : "NULL")},
                        '{Sanitizer.Sanitize(map.RoleId)}',
                        '{Sanitizer.Sanitize(map.PermissionId)}',
                        {(map.Active ? 1 : 0)},
                        '{Converters.ToTimestamp(map.CreatedUtc)}',
                        '{Converters.ToTimestamp(map.LastUpdateUtc)}');
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.RolePermissionMapFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<Permission>> ReadPermissionsForRole(string roleId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(roleId)) throw new ArgumentNullException(nameof(roleId));
            token.ThrowIfCancellationRequested();

            string query = $@"
                SELECT p.* FROM [permissions] p
                INNER JOIN [rolepermissionmaps] m ON m.[permissionid] = p.[id]
                WHERE m.[roleid] = '{Sanitizer.Sanitize(roleId)}'
                ORDER BY p.[createdutc] DESC;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<Permission> permissions = new List<Permission>();
            foreach (DataRow row in result.Rows)
                permissions.Add(Converters.PermissionFromDataRow(row));

            return permissions;
        }

        public async Task DeleteRolePermissionMap(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM [rolepermissionmaps] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        public async Task DeleteRolePermissionMapsByRole(string roleId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(roleId)) throw new ArgumentNullException(nameof(roleId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM [rolepermissionmaps] WHERE [roleid] = '{Sanitizer.Sanitize(roleId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        #endregion

        #region User-Role-Assignments

        public async Task<UserRoleAssignment> CreateUserRoleAssignment(UserRoleAssignment assignment, CancellationToken token = default)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO [userroleassignments] ([id], [tenantid], [userid], [roleid], [rolename], [resourcescope], [resourceid], [inheritstochildren], [active], [createdutc], [lastupdateutc])
                OUTPUT INSERTED.*
                VALUES ('{Sanitizer.Sanitize(assignment.Id)}',
                        '{Sanitizer.Sanitize(assignment.TenantId)}',
                        '{Sanitizer.Sanitize(assignment.UserId)}',
                        {(assignment.RoleId != null ? $"'{Sanitizer.Sanitize(assignment.RoleId)}'" : "NULL")},
                        {(assignment.RoleName != null ? $"N'{Sanitizer.Sanitize(assignment.RoleName)}'" : "NULL")},
                        {(int)assignment.ResourceScope},
                        {(assignment.ResourceId != null ? $"'{Sanitizer.Sanitize(assignment.ResourceId)}'" : "NULL")},
                        {(assignment.InheritsToChildren ? 1 : 0)},
                        {(assignment.Active ? 1 : 0)},
                        '{Converters.ToTimestamp(assignment.CreatedUtc)}',
                        '{Converters.ToTimestamp(assignment.LastUpdateUtc)}');
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.UserRoleAssignmentFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<UserRoleAssignment> ReadUserRoleAssignmentById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [userroleassignments] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.UserRoleAssignmentFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<UserRoleAssignment>> ReadUserRoleAssignments(string tenantId, string userId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [userroleassignments] WHERE [tenantid] = '{Sanitizer.Sanitize(tenantId)}' AND [userid] = '{Sanitizer.Sanitize(userId)}' ORDER BY [createdutc] DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<UserRoleAssignment> assignments = new List<UserRoleAssignment>();
            foreach (DataRow row in result.Rows)
                assignments.Add(Converters.UserRoleAssignmentFromDataRow(row));

            return assignments;
        }

        public async Task<List<UserRoleAssignment>> ReadAllUserRoleAssignments(string tenantId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [userroleassignments] WHERE [tenantid] = '{Sanitizer.Sanitize(tenantId)}' ORDER BY [createdutc] DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<UserRoleAssignment> assignments = new List<UserRoleAssignment>();
            foreach (DataRow row in result.Rows)
                assignments.Add(Converters.UserRoleAssignmentFromDataRow(row));

            return assignments;
        }

        public async Task DeleteUserRoleAssignment(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM [userroleassignments] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        #endregion

        #region Credential-Scope-Assignments

        public async Task<CredentialScopeAssignment> CreateCredentialScopeAssignment(CredentialScopeAssignment assignment, CancellationToken token = default)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO [credentialscopeassignments] ([id], [tenantid], [credentialid], [roleid], [rolename], [resourcescope], [resourceid], [permissions], [resourcetypes], [active], [createdutc], [lastupdateutc])
                OUTPUT INSERTED.*
                VALUES ('{Sanitizer.Sanitize(assignment.Id)}',
                        '{Sanitizer.Sanitize(assignment.TenantId)}',
                        '{Sanitizer.Sanitize(assignment.CredentialId)}',
                        {(assignment.RoleId != null ? $"'{Sanitizer.Sanitize(assignment.RoleId)}'" : "NULL")},
                        {(assignment.RoleName != null ? $"N'{Sanitizer.Sanitize(assignment.RoleName)}'" : "NULL")},
                        {(int)assignment.ResourceScope},
                        {(assignment.ResourceId != null ? $"'{Sanitizer.Sanitize(assignment.ResourceId)}'" : "NULL")},
                        N'{Sanitizer.Sanitize(SerializeEnumNames(assignment.Permissions))}',
                        N'{Sanitizer.Sanitize(SerializeEnumNames(assignment.ResourceTypes))}',
                        {(assignment.Active ? 1 : 0)},
                        '{Converters.ToTimestamp(assignment.CreatedUtc)}',
                        '{Converters.ToTimestamp(assignment.LastUpdateUtc)}');
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.CredentialScopeAssignmentFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<CredentialScopeAssignment> ReadCredentialScopeAssignmentById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [credentialscopeassignments] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.CredentialScopeAssignmentFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<CredentialScopeAssignment>> ReadCredentialScopeAssignments(string tenantId, string credentialId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (string.IsNullOrWhiteSpace(credentialId)) throw new ArgumentNullException(nameof(credentialId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [credentialscopeassignments] WHERE [tenantid] = '{Sanitizer.Sanitize(tenantId)}' AND [credentialid] = '{Sanitizer.Sanitize(credentialId)}' ORDER BY [createdutc] DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<CredentialScopeAssignment> assignments = new List<CredentialScopeAssignment>();
            foreach (DataRow row in result.Rows)
                assignments.Add(Converters.CredentialScopeAssignmentFromDataRow(row));

            return assignments;
        }

        public async Task DeleteCredentialScopeAssignment(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM [credentialscopeassignments] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private static string SerializeEnumNames<TEnum>(IEnumerable<TEnum> values) where TEnum : struct, Enum
        {
            List<string> names = new List<string>();
            if (values != null)
            {
                foreach (TEnum value in values)
                    names.Add(value.ToString());
            }

            return JsonSerializer.Serialize(names);
        }

        #endregion
    }
}
