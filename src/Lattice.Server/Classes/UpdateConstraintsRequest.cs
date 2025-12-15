namespace Lattice.Server.Classes
{
    using System.Collections.Generic;
    using Lattice.Core.Models;

    /// <summary>
    /// Request model for updating collection schema constraints.
    /// </summary>
    public class UpdateConstraintsRequest
    {
        /// <summary>
        /// New schema enforcement mode.
        /// </summary>
        public SchemaEnforcementMode SchemaEnforcementMode { get; set; }

        /// <summary>
        /// New field constraints (replaces existing).
        /// </summary>
        public List<FieldConstraint>? FieldConstraints { get; set; }
    }
}
