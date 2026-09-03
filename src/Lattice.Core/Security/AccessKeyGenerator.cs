namespace Lattice.Core.Security
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Generates credential access keys and session token nonces from a cryptographically secure source.
    /// </summary>
    public static class AccessKeyGenerator
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        /// <summary>
        /// Generate a new access key of the form <c>access_</c> followed by high-entropy characters.
        /// </summary>
        /// <param name="length">The number of random characters after the prefix. Default 40, minimum 32.</param>
        /// <returns>A new access key.</returns>
        public static string NewAccessKey(int length = 40)
        {
            if (length < 32) length = 32;
            return "access_" + RandomString(length);
        }

        /// <summary>
        /// Generate a random alphanumeric string of the requested length.
        /// </summary>
        /// <param name="length">The length. Minimum 1.</param>
        /// <returns>A random string.</returns>
        public static string RandomString(int length)
        {
            if (length < 1) length = 1;
            StringBuilder builder = new StringBuilder(length);
            byte[] buffer = RandomNumberGenerator.GetBytes(length);
            for (int i = 0; i < length; i++)
            {
                builder.Append(Alphabet[buffer[i] % Alphabet.Length]);
            }
            return builder.ToString();
        }
    }
}
