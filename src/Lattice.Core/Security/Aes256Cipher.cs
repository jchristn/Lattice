namespace Lattice.Core.Security
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// AES-256-CBC encryption with a fresh random IV per operation. The IV is prepended to the
    /// ciphertext. The 256-bit key is derived from a configured secret string via SHA-256.
    /// </summary>
    public sealed class Aes256Cipher
    {
        #region Private-Members

        private readonly byte[] _Key;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the cipher from a secret string. The secret is hashed with SHA-256 to produce the
        /// 256-bit key, so any non-empty string is acceptable.
        /// </summary>
        /// <param name="secret">The server-side secret. Must be non-null and non-empty.</param>
        /// <exception cref="ArgumentNullException">Thrown when the secret is null or empty.</exception>
        public Aes256Cipher(string secret)
        {
            if (String.IsNullOrEmpty(secret)) throw new ArgumentNullException(nameof(secret));
            _Key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Encrypt a UTF-8 string, returning base64 of (IV || ciphertext).
        /// </summary>
        /// <param name="plaintext">Plaintext to encrypt. Must be non-null.</param>
        /// <returns>Base64-encoded IV and ciphertext.</returns>
        /// <exception cref="ArgumentNullException">Thrown when plaintext is null.</exception>
        public string Encrypt(string plaintext)
        {
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));

            using (Aes aes = Aes.Create())
            {
                aes.Key = _Key;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (MemoryStream output = new MemoryStream())
                {
                    output.Write(iv, 0, iv.Length);
                    using (CryptoStream cryptoStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
                        cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                        cryptoStream.FlushFinalBlock();
                    }
                    return Convert.ToBase64String(output.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypt a value produced by <see cref="Encrypt"/>. Returns null when the input is malformed or
        /// cannot be decrypted.
        /// </summary>
        /// <param name="encoded">Base64-encoded IV and ciphertext.</param>
        /// <returns>The decrypted plaintext, or null.</returns>
        public string Decrypt(string encoded)
        {
            if (String.IsNullOrEmpty(encoded)) return null;

            try
            {
                byte[] all = Convert.FromBase64String(encoded);
                if (all.Length <= 16) return null;

                byte[] iv = new byte[16];
                Buffer.BlockCopy(all, 0, iv, 0, 16);
                byte[] cipher = new byte[all.Length - 16];
                Buffer.BlockCopy(all, 16, cipher, 0, cipher.Length);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = _Key;
                    aes.IV = iv;
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    using (MemoryStream input = new MemoryStream(cipher))
                    using (CryptoStream cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
                    using (StreamReader reader = new StreamReader(cryptoStream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
