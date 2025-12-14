namespace Lattice.Server.Classes
{
    using System.Text.Json.Serialization;
    using Lattice.Core.Search;

    /// <summary>
    /// Request model for a search filter.
    /// </summary>
    public class SearchFilterRequest
    {
        #region Public-Members

        /// <summary>
        /// Field name to filter on.
        /// </summary>
        [JsonPropertyName("field")]
        public string Field { get; set; } = null!;

        /// <summary>
        /// Filter condition.
        /// </summary>
        [JsonPropertyName("condition")]
        public SearchConditionEnum Condition { get; set; } = SearchConditionEnum.Equals;

        /// <summary>
        /// Value to filter by.
        /// </summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SearchFilterRequest()
        {
        }

        #endregion
    }
}
