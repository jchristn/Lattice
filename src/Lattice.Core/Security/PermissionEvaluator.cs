namespace Lattice.Core.Security
{
    using System;
    using System.Collections.Generic;
    using Lattice.Core.Models;

    /// <summary>
    /// Evaluates a request against a principal's effective grants using deny-over-permit ordering: an
    /// explicit Deny match denies, otherwise a Permit match permits, otherwise the request is denied
    /// implicitly.
    /// </summary>
    public static class PermissionEvaluator
    {
        /// <summary>
        /// Evaluate whether the grants permit an operation on a resource type and optional resource id.
        /// </summary>
        /// <param name="grants">The principal's effective grants.</param>
        /// <param name="resourceType">The resource type requested.</param>
        /// <param name="operation">The operation requested.</param>
        /// <param name="resourceId">The specific resource id, or null.</param>
        /// <returns>The verdict.</returns>
        public static AuthorizationVerdict Evaluate(
            IEnumerable<EffectiveGrant> grants,
            ResourceType resourceType,
            OperationType operation,
            string resourceId)
        {
            if (grants == null) return AuthorizationVerdict.DeniedImplicit;

            bool permitted = false;

            foreach (EffectiveGrant grant in grants)
            {
                if (!ScopeMatches(grant, resourceId)) continue;
                if (!ResourceMatches(grant, resourceType)) continue;
                if (!OperationMatches(grant, operation)) continue;

                if (grant.PermissionType == PermissionType.Deny)
                {
                    return AuthorizationVerdict.DeniedExplicit;
                }

                permitted = true;
            }

            return permitted ? AuthorizationVerdict.Permitted : AuthorizationVerdict.DeniedImplicit;
        }

        private static bool ScopeMatches(EffectiveGrant grant, string resourceId)
        {
            if (grant.ResourceScope == ResourceScope.Tenant)
            {
                // A tenant-scoped grant applies to any resource in the tenant when it inherits to children,
                // or to tenant-level operations (no specific resource) otherwise.
                return grant.InheritsToChildren || String.IsNullOrEmpty(resourceId);
            }

            // Resource-scoped: must target the specific resource.
            return !String.IsNullOrEmpty(grant.ResourceId)
                && String.Equals(grant.ResourceId, resourceId, StringComparison.Ordinal);
        }

        private static bool ResourceMatches(EffectiveGrant grant, ResourceType resourceType)
        {
            if (grant.ResourceTypes == null) return false;
            return grant.ResourceTypes.Contains(ResourceType.All) || grant.ResourceTypes.Contains(resourceType);
        }

        private static bool OperationMatches(EffectiveGrant grant, OperationType operation)
        {
            if (grant.OperationTypes == null) return false;
            if (grant.OperationTypes.Contains(OperationType.All)) return true;
            if (grant.OperationTypes.Contains(operation)) return true;

            // Write is shorthand that expands to Create, Update, and Delete.
            if ((operation == OperationType.Create || operation == OperationType.Update || operation == OperationType.Delete)
                && grant.OperationTypes.Contains(OperationType.Write))
            {
                return true;
            }

            return false;
        }
    }
}
