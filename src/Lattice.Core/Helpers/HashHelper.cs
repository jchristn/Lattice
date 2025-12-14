namespace Lattice.Core.Helpers
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Helper class for generating hashes.
    /// </summary>
    public static class HashHelper
    {
        #region Public-Methods

        /// <summary>
        /// Compute MD5 hash of a string.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>MD5 hash as lowercase hex string.</returns>
        public static string ComputeMd5Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = MD5.HashData(inputBytes);
            return Convert.ToHexStringLower(hashBytes);
        }

        /// <summary>
        /// Compute SHA256 hash of a string.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>SHA256 hash as lowercase hex string.</returns>
        public static string ComputeSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexStringLower(hashBytes);
        }

        /// <summary>
        /// Generate an index table name from a key.
        /// </summary>
        /// <param name="key">The dot-notation key path.</param>
        /// <returns>Index table name in format index_{hash}.</returns>
        public static string GenerateIndexTableName(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            string hash = ComputeMd5Hash(key);
            return $"index_{hash}";
        }

        #endregion
    }
}
