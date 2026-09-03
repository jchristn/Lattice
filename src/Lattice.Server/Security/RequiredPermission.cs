namespace Lattice.Server.Security
{
    using Lattice.Core.Models;

    /// <summary>
    /// The permission a request requires: either public (no authentication), any-authenticated, or a
    /// specific resource type and operation evaluated against the caller's grants.
    /// </summary>
    public class RequiredPermission
    {
        /// <summary>
        /// Whether the route is public and needs no authentication.
        /// </summary>
        public bool Public { get; set; } = false;

        /// <summary>
        /// Whether any authenticated principal may access the route, regardless of grants.
        /// </summary>
        public bool AnyAuthenticated { get; set; } = false;

        /// <summary>
        /// The resource type the request targets.
        /// </summary>
        public ResourceType ResourceType { get; set; } = ResourceType.All;

        /// <summary>
        /// The operation the request performs.
        /// </summary>
        public OperationType Operation { get; set; } = OperationType.Read;

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public RequiredPermission()
        {
        }

        /// <summary>
        /// Instantiate with a resource type and operation.
        /// </summary>
        /// <param name="resourceType">Resource type.</param>
        /// <param name="operation">Operation.</param>
        public RequiredPermission(ResourceType resourceType, OperationType operation)
        {
            ResourceType = resourceType;
            Operation = operation;
        }

        /// <summary>
        /// Create a public (no-auth) requirement.
        /// </summary>
        /// <returns>A public requirement.</returns>
        public static RequiredPermission ForPublic()
        {
            return new RequiredPermission { Public = true };
        }

        /// <summary>
        /// Create an any-authenticated requirement.
        /// </summary>
        /// <returns>An any-authenticated requirement.</returns>
        public static RequiredPermission ForAnyAuthenticated()
        {
            return new RequiredPermission { AnyAuthenticated = true };
        }
    }
}
