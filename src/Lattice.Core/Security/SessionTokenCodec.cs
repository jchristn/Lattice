namespace Lattice.Core.Security
{
    using System;
    using System.Text.Json;

    /// <summary>
    /// Encodes and decodes opaque session tokens. A token is the AES-256 encryption of a serialized
    /// <see cref="TokenPayload"/>; clients never see its contents.
    /// </summary>
    public sealed class SessionTokenCodec
    {
        #region Private-Members

        private readonly Aes256Cipher _Cipher;

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the codec with the server-side token secret.
        /// </summary>
        /// <param name="secret">The token signing/encryption secret. Must be non-null and non-empty.</param>
        /// <exception cref="ArgumentNullException">Thrown when the secret is null or empty.</exception>
        public SessionTokenCodec(string secret)
        {
            if (String.IsNullOrEmpty(secret)) throw new ArgumentNullException(nameof(secret));
            _Cipher = new Aes256Cipher(secret);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Serialize and encrypt a payload into an opaque token string.
        /// </summary>
        /// <param name="payload">The payload to encode. Must be non-null.</param>
        /// <returns>The opaque token.</returns>
        /// <exception cref="ArgumentNullException">Thrown when payload is null.</exception>
        public string Encode(TokenPayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            string json = JsonSerializer.Serialize(payload, _JsonOptions);
            return _Cipher.Encrypt(json);
        }

        /// <summary>
        /// Decrypt and deserialize a token. Returns null when the token is malformed or cannot be decrypted.
        /// Expiry and session validity are checked separately by the authentication service.
        /// </summary>
        /// <param name="token">The opaque token.</param>
        /// <returns>The decoded payload, or null.</returns>
        public TokenPayload Decode(string token)
        {
            if (String.IsNullOrEmpty(token)) return null;
            string json = _Cipher.Decrypt(token);
            if (String.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<TokenPayload>(json, _JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
