namespace Lattice.Core.Flattening
{
    /// <summary>
    /// Represents a single flattened key-value pair from a JSON document.
    /// </summary>
    public class FlattenedValue
    {
        #region Public-Members

        /// <summary>
        /// The dot-notation key path (e.g., "Person.Addresses").
        /// </summary>
        public string Key { get; set; } = null;

        /// <summary>
        /// Array position if this value is from an array element, null otherwise.
        /// </summary>
        public int? Position { get; set; } = null;

        /// <summary>
        /// The string representation of the value.
        /// </summary>
        public string Value { get; set; } = null;

        /// <summary>
        /// The inferred data type (string, integer, number, boolean, null).
        /// </summary>
        public string DataType { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public FlattenedValue()
        {
        }

        /// <summary>
        /// Instantiate the object with values.
        /// </summary>
        /// <param name="key">The key path.</param>
        /// <param name="value">The value.</param>
        /// <param name="dataType">The data type.</param>
        /// <param name="position">Array position if applicable.</param>
        public FlattenedValue(string key, string value, string dataType, int? position = null)
        {
            Key = key;
            Value = value;
            DataType = dataType;
            Position = position;
        }

        #endregion
    }
}
