namespace Lattice.Core.Security
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Hashes passwords and access keys as SHA-256 hex and compares secrets in constant time.
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Compute the lowercase SHA-256 hex hash of a value.
        /// </summary>
        /// <param name="value">Value to hash. Null is treated as empty.</param>
        /// <returns>The 64-character lowercase hex hash.</returns>
        public static string Sha256Hex(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? String.Empty);
            byte[] hash = SHA256.HashData(bytes);
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        /// <summary>
        /// Compare two strings in constant time to avoid timing side channels. Returns false if either
        /// value is null.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>True when the values are equal.</returns>
        public static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            byte[] aBytes = Encoding.UTF8.GetBytes(a);
            byte[] bBytes = Encoding.UTF8.GetBytes(b);
            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }
    }
}
