namespace Lattice.Server.Classes
{
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
        public string Field { get; set; } = null!;

        /// <summary>
        /// Filter condition.
        /// </summary>
        public SearchConditionEnum Condition { get; set; } = SearchConditionEnum.Equals;

        /// <summary>
        /// Value to filter by.
        /// </summary>
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
