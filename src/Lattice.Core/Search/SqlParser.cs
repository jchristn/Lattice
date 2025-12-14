namespace Lattice.Core.Search
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Parser for SQL-like query expressions.
    /// Supports: SELECT * FROM documents WHERE field = 'value' AND/OR ...
    /// </summary>
    public class SqlParser
    {
        #region Public-Methods

        /// <summary>
        /// Parse a SQL-like expression into a SearchQuery.
        /// </summary>
        /// <param name="sql">SQL expression (e.g., "SELECT * FROM documents WHERE Person.First = 'Joel'").</param>
        /// <returns>SearchQuery with filters populated.</returns>
        public SearchQuery Parse(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentNullException(nameof(sql));

            SearchQuery query = new SearchQuery();

            // Normalize whitespace
            sql = Regex.Replace(sql.Trim(), @"\s+", " ");

            // Parse the WHERE clause
            int whereIndex = sql.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase);
            if (whereIndex >= 0)
            {
                string whereClause = sql.Substring(whereIndex + 7).Trim();
                query.Filters = ParseWhereClause(whereClause);
            }

            // Parse ORDER BY clause
            int orderByIndex = sql.IndexOf(" ORDER BY ", StringComparison.OrdinalIgnoreCase);
            if (orderByIndex >= 0)
            {
                string orderByClause = sql.Substring(orderByIndex + 10).Trim();
                query.Ordering = ParseOrderBy(orderByClause);
            }

            // Parse LIMIT clause
            int limitIndex = sql.IndexOf(" LIMIT ", StringComparison.OrdinalIgnoreCase);
            if (limitIndex >= 0)
            {
                string limitClause = sql.Substring(limitIndex + 7).Trim();
                Match match = Regex.Match(limitClause, @"^(\d+)");
                if (match.Success)
                {
                    query.MaxResults = int.Parse(match.Groups[1].Value);
                }
            }

            // Parse OFFSET clause
            int offsetIndex = sql.IndexOf(" OFFSET ", StringComparison.OrdinalIgnoreCase);
            if (offsetIndex >= 0)
            {
                string offsetClause = sql.Substring(offsetIndex + 8).Trim();
                Match match = Regex.Match(offsetClause, @"^(\d+)");
                if (match.Success)
                {
                    query.Skip = int.Parse(match.Groups[1].Value);
                }
            }

            return query;
        }

        /// <summary>
        /// Parse a WHERE clause into a list of search filters.
        /// </summary>
        /// <param name="whereClause">WHERE clause without the WHERE keyword.</param>
        /// <returns>List of search filters.</returns>
        public List<SearchFilter> ParseWhereClause(string whereClause)
        {
            List<SearchFilter> filters = new List<SearchFilter>();

            if (string.IsNullOrWhiteSpace(whereClause))
                return filters;

            // Remove ORDER BY, LIMIT, OFFSET from where clause
            int orderByPos = whereClause.IndexOf(" ORDER BY ", StringComparison.OrdinalIgnoreCase);
            if (orderByPos >= 0) whereClause = whereClause.Substring(0, orderByPos);

            int limitPos = whereClause.IndexOf(" LIMIT ", StringComparison.OrdinalIgnoreCase);
            if (limitPos >= 0) whereClause = whereClause.Substring(0, limitPos);

            // Split by AND (for now, simple AND-only logic)
            // Note: A more sophisticated parser would handle OR and parentheses
            List<string> conditions = SplitConditions(whereClause);

            foreach (string condition in conditions)
            {
                SearchFilter filter = ParseCondition(condition.Trim());
                if (filter != null)
                {
                    filters.Add(filter);
                }
            }

            return filters;
        }

        #endregion

        #region Private-Methods

        private List<string> SplitConditions(string whereClause)
        {
            List<string> conditions = new List<string>();

            // Simple split by AND (case-insensitive)
            // This is a simplified implementation - a full parser would handle nested expressions
            string[] parts = Regex.Split(whereClause, @"\s+AND\s+", RegexOptions.IgnoreCase);

            foreach (string part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    conditions.Add(part.Trim());
                }
            }

            return conditions;
        }

        private SearchFilter ParseCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return null;

            // Try IS NULL / IS NOT NULL first
            Match isNullMatch = Regex.Match(condition, @"^(.+?)\s+IS\s+NULL$", RegexOptions.IgnoreCase);
            if (isNullMatch.Success)
            {
                return new SearchFilter(isNullMatch.Groups[1].Value.Trim(), SearchConditionEnum.IsNull);
            }

            Match isNotNullMatch = Regex.Match(condition, @"^(.+?)\s+IS\s+NOT\s+NULL$", RegexOptions.IgnoreCase);
            if (isNotNullMatch.Success)
            {
                return new SearchFilter(isNotNullMatch.Groups[1].Value.Trim(), SearchConditionEnum.IsNotNull);
            }

            // Try LIKE
            Match likeMatch = Regex.Match(condition, @"^(.+?)\s+LIKE\s+'(.+)'$", RegexOptions.IgnoreCase);
            if (likeMatch.Success)
            {
                string field = likeMatch.Groups[1].Value.Trim();
                string pattern = likeMatch.Groups[2].Value;

                // Convert LIKE patterns to specific conditions
                if (pattern.StartsWith("%") && pattern.EndsWith("%"))
                {
                    return new SearchFilter(field, SearchConditionEnum.Contains, pattern.Trim('%'));
                }
                else if (pattern.StartsWith("%"))
                {
                    return new SearchFilter(field, SearchConditionEnum.EndsWith, pattern.TrimStart('%'));
                }
                else if (pattern.EndsWith("%"))
                {
                    return new SearchFilter(field, SearchConditionEnum.StartsWith, pattern.TrimEnd('%'));
                }
                else
                {
                    return new SearchFilter(field, SearchConditionEnum.Like, pattern);
                }
            }

            // Try comparison operators (>=, <=, !=, <>, =, >, <)
            // Match single-quoted strings: field = 'value'
            Match singleQuoteMatch = Regex.Match(condition, @"^(.+?)\s*(>=|<=|!=|<>|=|>|<)\s*'((?:''|[^'])*)'$");
            if (singleQuoteMatch.Success)
            {
                string field = singleQuoteMatch.Groups[1].Value.Trim();
                string op = singleQuoteMatch.Groups[2].Value;
                // Unescape doubled single quotes
                string value = singleQuoteMatch.Groups[3].Value.Replace("''", "'");

                SearchConditionEnum searchCondition = op switch
                {
                    "=" => SearchConditionEnum.Equals,
                    "!=" => SearchConditionEnum.NotEquals,
                    "<>" => SearchConditionEnum.NotEquals,
                    ">" => SearchConditionEnum.GreaterThan,
                    ">=" => SearchConditionEnum.GreaterThanOrEqualTo,
                    "<" => SearchConditionEnum.LessThan,
                    "<=" => SearchConditionEnum.LessThanOrEqualTo,
                    _ => SearchConditionEnum.Equals
                };

                return new SearchFilter(field, searchCondition, value);
            }

            // Match double-quoted strings: field = "value"
            Match doubleQuoteMatch = Regex.Match(condition, @"^(.+?)\s*(>=|<=|!=|<>|=|>|<)\s*""((?:""""|[^""])*)""$");
            if (doubleQuoteMatch.Success)
            {
                string field = doubleQuoteMatch.Groups[1].Value.Trim();
                string op = doubleQuoteMatch.Groups[2].Value;
                // Unescape doubled double quotes
                string value = doubleQuoteMatch.Groups[3].Value.Replace("\"\"", "\"");

                SearchConditionEnum searchCondition = op switch
                {
                    "=" => SearchConditionEnum.Equals,
                    "!=" => SearchConditionEnum.NotEquals,
                    "<>" => SearchConditionEnum.NotEquals,
                    ">" => SearchConditionEnum.GreaterThan,
                    ">=" => SearchConditionEnum.GreaterThanOrEqualTo,
                    "<" => SearchConditionEnum.LessThan,
                    "<=" => SearchConditionEnum.LessThanOrEqualTo,
                    _ => SearchConditionEnum.Equals
                };

                return new SearchFilter(field, searchCondition, value);
            }

            // Match unquoted values (numbers or simple identifiers): field = 123
            Match unquotedMatch = Regex.Match(condition, @"^(.+?)\s*(>=|<=|!=|<>|=|>|<)\s*(\S+)$");
            if (unquotedMatch.Success)
            {
                string field = unquotedMatch.Groups[1].Value.Trim();
                string op = unquotedMatch.Groups[2].Value;
                string value = unquotedMatch.Groups[3].Value.Trim();

                SearchConditionEnum searchCondition = op switch
                {
                    "=" => SearchConditionEnum.Equals,
                    "!=" => SearchConditionEnum.NotEquals,
                    "<>" => SearchConditionEnum.NotEquals,
                    ">" => SearchConditionEnum.GreaterThan,
                    ">=" => SearchConditionEnum.GreaterThanOrEqualTo,
                    "<" => SearchConditionEnum.LessThan,
                    "<=" => SearchConditionEnum.LessThanOrEqualTo,
                    _ => SearchConditionEnum.Equals
                };

                return new SearchFilter(field, searchCondition, value);
            }

            return null;
        }

        private EnumerationOrderEnum ParseOrderBy(string orderByClause)
        {
            if (string.IsNullOrWhiteSpace(orderByClause))
                return EnumerationOrderEnum.CreatedDescending;

            // Remove LIMIT from order by
            int limitPos = orderByClause.IndexOf(" LIMIT ", StringComparison.OrdinalIgnoreCase);
            if (limitPos >= 0) orderByClause = orderByClause.Substring(0, limitPos);

            orderByClause = orderByClause.Trim();

            // Check for ASC/DESC
            bool descending = orderByClause.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase);
            bool ascending = orderByClause.EndsWith(" ASC", StringComparison.OrdinalIgnoreCase);

            // Extract field name
            string field = Regex.Replace(orderByClause, @"\s+(ASC|DESC)$", "", RegexOptions.IgnoreCase).Trim().ToLower();

            return field switch
            {
                "createdutc" when descending => EnumerationOrderEnum.CreatedDescending,
                "createdutc" when ascending => EnumerationOrderEnum.CreatedAscending,
                "createdutc" => EnumerationOrderEnum.CreatedDescending,
                "lastupdateutc" when descending => EnumerationOrderEnum.LastUpdateDescending,
                "lastupdateutc" when ascending => EnumerationOrderEnum.LastUpdateAscending,
                "lastupdateutc" => EnumerationOrderEnum.LastUpdateDescending,
                "name" when descending => EnumerationOrderEnum.NameDescending,
                "name" when ascending => EnumerationOrderEnum.NameAscending,
                "name" => EnumerationOrderEnum.NameAscending,
                _ => descending ? EnumerationOrderEnum.CreatedDescending : EnumerationOrderEnum.CreatedAscending
            };
        }

        #endregion
    }
}
