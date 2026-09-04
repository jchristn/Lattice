namespace Lattice.Core.Security
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;

    /// <summary>
    /// Seeds the security model on startup: the built-in roles and their permissions (idempotent,
    /// preserving identifiers), and — when no tenants exist — a default tenant, administrator, and
    /// credential.
    /// </summary>
    public static class FirstBootSeeder
    {
        /// <summary>
        /// Ensure built-in roles exist and, on an empty database, create the default tenant, admin, and
        /// credential.
        /// </summary>
        /// <param name="client">The Lattice client exposing the security repositories.</param>
        /// <param name="defaultTenantName">Name for the default tenant.</param>
        /// <param name="adminEmail">Email for the default administrator.</param>
        /// <param name="adminPassword">Password for the default administrator.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The seed result.</returns>
        public static async Task<SeedResult> SeedAsync(
            LatticeClient client,
            string defaultTenantName,
            string adminEmail,
            string adminPassword,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            await SeedBuiltInRolesAsync(client, token).ConfigureAwait(false);

            SeedResult result = new SeedResult();

            long tenantCount = await client.Tenants.Count(token).ConfigureAwait(false);
            if (tenantCount > 0) return result;

            DateTime now = DateTime.UtcNow;

            Tenant tenant = new Tenant
            {
                Id = IdGenerator.NewTenantId(),
                Name = string.IsNullOrWhiteSpace(defaultTenantName) ? "Default Tenant" : defaultTenantName,
                Active = true,
                IsProtected = true,
                CreatedUtc = now,
                LastUpdateUtc = now
            };
            await client.Tenants.Create(tenant, token).ConfigureAwait(false);

            User admin = new User
            {
                Id = IdGenerator.NewUserId(),
                TenantId = tenant.Id,
                FirstName = "Default",
                LastName = "Admin",
                Email = string.IsNullOrWhiteSpace(adminEmail) ? "admin@lattice" : adminEmail,
                PasswordSha256 = PasswordHasher.Sha256Hex(string.IsNullOrEmpty(adminPassword) ? "password" : adminPassword),
                IsAdmin = true,
                IsTenantAdmin = true,
                Active = true,
                IsProtected = true,
                CreatedUtc = now,
                LastUpdateUtc = now
            };
            await client.Users.Create(admin, token).ConfigureAwait(false);

            string rawAccessKey = AccessKeyGenerator.NewAccessKey();
            Credential credential = new Credential
            {
                Id = IdGenerator.NewCredentialId(),
                TenantId = tenant.Id,
                UserId = admin.Id,
                Name = "Default API Key",
                AccessKey = rawAccessKey,
                AccessKeySha256 = PasswordHasher.Sha256Hex(rawAccessKey),
                AccessKeyLast4 = rawAccessKey.Substring(rawAccessKey.Length - 4),
                Active = true,
                IsProtected = true,
                CreatedUtc = now,
                LastUpdateUtc = now
            };
            await client.Credentials.Create(credential, token).ConfigureAwait(false);

            // Give the default administrator an explicit TenantAdmin assignment so the tenant has a visible,
            // non-empty set of role assignments out of the box (the admin also bypasses RBAC as a system admin).
            UserRole tenantAdminRole = await client.Roles.ReadRoleByName(null, "TenantAdmin", token).ConfigureAwait(false);
            if (tenantAdminRole != null)
            {
                UserRoleAssignment assignment = new UserRoleAssignment
                {
                    Id = IdGenerator.NewUserRoleAssignmentId(),
                    TenantId = tenant.Id,
                    UserId = admin.Id,
                    RoleId = tenantAdminRole.Id,
                    RoleName = tenantAdminRole.Name,
                    ResourceScope = ResourceScope.Tenant,
                    InheritsToChildren = true,
                    Active = true,
                    CreatedUtc = now,
                    LastUpdateUtc = now
                };
                await client.Roles.CreateUserRoleAssignment(assignment, token).ConfigureAwait(false);
            }

            result.CreatedDefaults = true;
            result.TenantId = tenant.Id;
            result.AdminEmail = admin.Email;
            result.DefaultAccessKey = rawAccessKey;
            return result;
        }

        private static async Task SeedBuiltInRolesAsync(LatticeClient client, CancellationToken token)
        {
            List<BuiltInRoleDefinition> definitions = BuiltInRoles.All();
            DateTime now = DateTime.UtcNow;

            foreach (BuiltInRoleDefinition definition in definitions)
            {
                UserRole existing = await client.Roles.ReadRoleByName(null, definition.Name, token).ConfigureAwait(false);
                if (existing != null) continue;

                UserRole role = new UserRole
                {
                    Id = IdGenerator.NewUserRoleId(),
                    TenantId = null,
                    Name = definition.Name,
                    IsBuiltIn = true,
                    Active = true,
                    IsProtected = true,
                    CreatedUtc = now,
                    LastUpdateUtc = now
                };
                await client.Roles.CreateRole(role, token).ConfigureAwait(false);

                foreach (BuiltInRolePermission grant in definition.Permissions)
                {
                    Permission permission = new Permission
                    {
                        Id = IdGenerator.NewPermissionId(),
                        TenantId = null,
                        Name = definition.Name + " grant",
                        ResourceTypes = new List<ResourceType>(grant.ResourceTypes),
                        OperationTypes = new List<OperationType>(grant.OperationTypes),
                        PermissionType = grant.PermissionType,
                        Active = true,
                        IsProtected = true,
                        CreatedUtc = now,
                        LastUpdateUtc = now
                    };
                    await client.Roles.CreatePermission(permission, token).ConfigureAwait(false);

                    RolePermissionMap map = new RolePermissionMap
                    {
                        Id = IdGenerator.NewRolePermissionMapId(),
                        TenantId = null,
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                        Active = true,
                        CreatedUtc = now,
                        LastUpdateUtc = now
                    };
                    await client.Roles.CreateRolePermissionMap(map, token).ConfigureAwait(false);
                }
            }
        }
    }
}
