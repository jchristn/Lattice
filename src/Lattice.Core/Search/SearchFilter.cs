namespace Lattice.Core.Search
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a search filter condition.
    /// </summary>
    public class SearchFilter
    {
        #region Public-Members

        /// <summary>
        /// The field/key to filter on (dot-notation path like "Person.First").
        /// </summary>
        [JsonPropertyName("field")]
        public string Field
        {
            get => _Field;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(Field));
                _Field = value;
            }
        }

        /// <summary>
        /// The condition operator.
        /// </summary>
        [JsonPropertyName("condition")]
        public SearchConditionEnum Condition { get; set; } = SearchConditionEnum.Equals;

        /// <summary>
        /// The value to compare against.
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = null;

        #endregion

        #region Private-Members

        private string _Field = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public SearchFilter()
        {
        }

        /// <summary>
        /// Instantiate the object with values.
        /// </summary>
        /// <param name="field">Field name.</param>
        /// <param name="condition">Condition operator.</param>
        /// <param name="value">Value to compare.</param>
        public SearchFilter(string field, SearchConditionEnum condition, string value = null)
        {
            Field = field;
            Condition = condition;
            Value = value;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Convert to SQL WHERE clause fragment.
        /// </summary>
        /// <param name="parameterName">Parameter name for parameterized queries.</param>
        /// <returns>SQL fragment.</returns>
        public string ToSqlCondition(string parameterName = "@value")
        {
            return Condition switch
            {
                SearchConditionEnum.Equals => $"value = {parameterName}",
                SearchConditionEnum.NotEquals => $"value != {parameterName}",
                SearchConditionEnum.GreaterThan => $"CAST(value AS REAL) > CAST({parameterName} AS REAL)",
                SearchConditionEnum.GreaterThanOrEqualTo => $"CAST(value AS REAL) >= CAST({parameterName} AS REAL)",
                SearchConditionEnum.LessThan => $"CAST(value AS REAL) < CAST({parameterName} AS REAL)",
                SearchConditionEnum.LessThanOrEqualTo => $"CAST(value AS REAL) <= CAST({parameterName} AS REAL)",
                SearchConditionEnum.IsNull => "value IS NULL",
                SearchConditionEnum.IsNotNull => "value IS NOT NULL",
                SearchConditionEnum.Contains => $"value LIKE '%' || {parameterName} || '%'",
                SearchConditionEnum.StartsWith => $"value LIKE {parameterName} || '%'",
                SearchConditionEnum.EndsWith => $"value LIKE '%' || {parameterName}",
                SearchConditionEnum.Like => $"value LIKE {parameterName}",
                _ => throw new NotSupportedException($"Condition {Condition} is not supported")
            };
        }

        #endregion
    }
}
