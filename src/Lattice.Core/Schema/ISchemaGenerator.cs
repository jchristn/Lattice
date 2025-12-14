namespace Lattice.Core.Schema
{
    using System.Collections.Generic;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for JSON schema generation and analysis.
    /// </summary>
    public interface ISchemaGenerator
    {
        /// <summary>
        /// Extract schema elements from a JSON string.
        /// </summary>
        /// <param name="json">JSON string to analyze.</param>
        /// <returns>List of schema elements representing the structure.</returns>
        List<SchemaElement> ExtractElements(string json);

        /// <summary>
        /// Compute a hash for schema elements for deduplication.
        /// </summary>
        /// <param name="elements">List of schema elements.</param>
        /// <returns>Hash string representing the schema structure.</returns>
        string ComputeSchemaHash(List<SchemaElement> elements);

        /// <summary>
        /// Check if two schemas match, optionally with flexibility for nullable fields.
        /// </summary>
        /// <param name="elementsA">First set of schema elements.</param>
        /// <param name="elementsB">Second set of schema elements.</param>
        /// <param name="allowFlexibility">If true, allow differences in nullable/optional fields.</param>
        /// <returns>True if schemas match according to criteria.</returns>
        bool SchemasMatch(List<SchemaElement> elementsA, List<SchemaElement> elementsB, bool allowFlexibility = true);
    }
}
