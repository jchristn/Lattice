namespace Lattice.Server.Classes
{
    /// <summary>Request body for updating a collection's descriptive fields. Only supplied fields are changed.</summary>
    public class UpdateCollectionRequest
    {
        /// <summary>New collection name, or null to leave unchanged.</summary>
        public string Name { get; set; } = null;

        /// <summary>New description, or null to leave unchanged.</summary>
        public string Description { get; set; } = null;
    }
}
