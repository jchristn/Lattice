namespace Lattice.Core.Repositories.SqlServer
{
    /// <summary>
    /// SQL sanitization utilities for SQL Server.
    /// </summary>
    internal static class Sanitizer
    {
        /// <summary>
        /// Sanitize a string for use in SQL.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>Sanitized string.</returns>
        internal static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            // SQL Server uses single quote doubling for escaping
            return input.Replace("'", "''");
        }

        /// <summary>
        /// Sanitize a table name (alphanumeric and underscore only).
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>Sanitized table name.</returns>
        internal static string SanitizeTableName(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return tableName;

            System.Text.StringBuilder result = new System.Text.StringBuilder();
            foreach (char c in tableName)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    result.Append(c);
            }
            return result.ToString();
        }
    }
}
