namespace Lattice.Core.Flattening
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for JSON document flattening.
    /// </summary>
    public interface IJsonFlattener
    {
        /// <summary>
        /// Flatten a JSON document into key-value pairs.
        /// </summary>
        /// <param name="json">JSON string to flatten.</param>
        /// <returns>List of flattened key-value pairs.</returns>
        List<FlattenedValue> Flatten(string json);
    }
}
