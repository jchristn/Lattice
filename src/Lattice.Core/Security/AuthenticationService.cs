namespace Lattice.Core.Security
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// Resolves a request to a principal. Two paths converge here: an interactive login that issues a
    /// session token, and a bearer value that is either a session token or a credential access key.
    /// </summary>
    public class AuthenticationService
    {
        #region Private-Members

        private readonly ITenantMethods _Tenants;
        private readonly IUserMethods _Users;
        private readonly ICredentialMethods _Credentials;
        private readonly IAuthSessionMethods _Sessions;
        private readonly SessionTokenCodec _Codec;
        private readonly int _SessionTtlMinutes;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the authentication service.
        /// </summary>
        /// <param name="tenants">Tenant repository.</param>
        /// <param name="users">User repository.</param>
        /// <param name="credentials">Credential repository.</param>
        /// <param name="sessions">Session repository.</param>
        /// <param name="codec">Session token codec.</param>
        /// <param name="sessionTtlMinutes">Session lifetime in minutes. Default 60, clamped to 5..1440.</param>
        public AuthenticationService(
            ITenantMethods tenants,
            IUserMethods users,
            ICredentialMethods credentials,
            IAuthSessionMethods sessions,
            SessionTokenCodec codec,
            int sessionTtlMinutes = 60)
        {
            _Tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
            _Users = users ?? throw new ArgumentNullException(nameof(users));
            _Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _Sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _SessionTtlMinutes = Math.Clamp(sessionTtlMinutes, 5, 1440);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Authenticate an interactive login. Returns null when the tenant, user, or password is invalid.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="email">User email.</param>
        /// <param name="password">User password (plaintext, compared as SHA-256).</param>
        /// <param name="sourceIp">Source IP of the request, or null.</param>
        /// <param name="userAgent">User agent of the request, or null.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A login result, or null on failure.</returns>
        public async Task<LoginResult> LoginAsync(
            string tenantId,
            string email,
            string password,
            string sourceIp = null,
            string userAgent = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId) || String.IsNullOrWhiteSpace(email) || password == null)
                return null;

            Tenant tenant = await _Tenants.ReadById(tenantId, token).ConfigureAwait(false);
            if (tenant == null || !tenant.Active) return null;

            User user = await _Users.ReadByEmail(tenantId, email, token).ConfigureAwait(false);
            if (user == null || !user.Active) return null;

            string presented = PasswordHasher.Sha256Hex(password);
            if (!PasswordHasher.ConstantTimeEquals(user.PasswordSha256, presented)) return null;

            DateTime now = DateTime.UtcNow;
            DateTime expires = now.AddMinutes(_SessionTtlMinutes);
            string tokenId = AccessKeyGenerator.RandomString(32);

            AuthSession session = new AuthSession
            {
                Id = IdGenerator.NewAuthSessionId(),
                TenantId = tenantId,
                PrincipalType = PrincipalType.User,
                UserId = user.Id,
                TokenId = tokenId,
                SourceIp = sourceIp,
                UserAgent = userAgent,
                ExpiresUtc = expires,
                Active = true,
                CreatedUtc = now,
                LastUpdateUtc = now
            };
            await _Sessions.Create(session, token).ConfigureAwait(false);

            TokenPayload payload = new TokenPayload
            {
                SessionId = session.Id,
                TokenId = tokenId,
                UserId = user.Id,
                TenantId = tenantId,
                IssuedUtc = now,
                ExpiresUtc = expires,
                Nonce = AccessKeyGenerator.RandomString(16)
            };

            LoginResult result = new LoginResult
            {
                Token = _Codec.Encode(payload),
                ExpiresUtc = expires,
                Caller = BuildUserCaller(user, session.Id)
            };
            return result;
        }

        /// <summary>
        /// Resolve which tenant(s) a set of login credentials belongs to, without issuing a token. Returns
        /// only the tenants in which an active user with this email has this password and the tenant is
        /// active — so a wrong password reveals nothing. Callers use this to infer the tenant when none was
        /// supplied (one match) or to prompt the user to choose (several matches).
        /// </summary>
        /// <param name="email">User email.</param>
        /// <param name="password">User password (plaintext, compared as SHA-256).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The matching tenants (possibly empty).</returns>
        public async Task<List<LoginTenantOption>> ResolveTenantsForLoginAsync(string email, string password, CancellationToken token = default)
        {
            List<LoginTenantOption> matches = new List<LoginTenantOption>();
            if (String.IsNullOrWhiteSpace(email) || password == null) return matches;

            List<User> users = await _Users.ReadByEmailAcrossTenants(email, token).ConfigureAwait(false);
            if (users == null || users.Count == 0) return matches;

            string presented = PasswordHasher.Sha256Hex(password);
            foreach (User user in users)
            {
                if (!user.Active) continue;
                if (!PasswordHasher.ConstantTimeEquals(user.PasswordSha256, presented)) continue;

                Tenant tenant = await _Tenants.ReadById(user.TenantId, token).ConfigureAwait(false);
                if (tenant == null || !tenant.Active) continue;

                bool already = false;
                foreach (LoginTenantOption option in matches)
                {
                    if (String.Equals(option.TenantId, tenant.Id, StringComparison.Ordinal)) { already = true; break; }
                }
                if (!already) matches.Add(new LoginTenantOption { TenantId = tenant.Id, TenantName = tenant.Name });
            }

            return matches;
        }

        /// <summary>
        /// Resolve a bearer value to a principal. The value is tried first as a session token and then as
        /// a credential access key. Returns null when it resolves to neither a valid session nor an active
        /// credential.
        /// </summary>
        /// <param name="bearer">The bearer value.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The resolved caller, or null.</returns>
        public async Task<CallerContext> AuthenticateBearerAsync(string bearer, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(bearer)) return null;

            CallerContext sessionCaller = await TryAuthenticateSessionAsync(bearer, token).ConfigureAwait(false);
            if (sessionCaller != null) return sessionCaller;

            return await TryAuthenticateCredentialAsync(bearer, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private async Task<CallerContext> TryAuthenticateSessionAsync(string bearer, CancellationToken token)
        {
            TokenPayload payload = _Codec.Decode(bearer);
            if (payload == null || String.IsNullOrEmpty(payload.TokenId)) return null;
            if (payload.ExpiresUtc <= DateTime.UtcNow) return null;

            AuthSession session = await _Sessions.ReadByTokenId(payload.TokenId, token).ConfigureAwait(false);
            if (session == null || !session.Active) return null;
            if (session.RevokedUtc != null) return null;
            if (session.ExpiresUtc <= DateTime.UtcNow) return null;
            if (!String.Equals(session.TenantId, payload.TenantId, StringComparison.Ordinal)) return null;

            User user = await _Users.ReadById(session.UserId, token).ConfigureAwait(false);
            if (user == null || !user.Active) return null;
            if (!String.Equals(user.TenantId, session.TenantId, StringComparison.Ordinal)) return null;

            Tenant tenant = await _Tenants.ReadById(session.TenantId, token).ConfigureAwait(false);
            if (tenant == null || !tenant.Active) return null;

            return BuildUserCaller(user, session.Id);
        }

        private async Task<CallerContext> TryAuthenticateCredentialAsync(string bearer, CancellationToken token)
        {
            string hash = PasswordHasher.Sha256Hex(bearer);
            Credential credential = await _Credentials.ReadByAccessKeyHash(hash, token).ConfigureAwait(false);
            if (credential == null || !credential.Active) return null;
            if (credential.ExpiresUtc != null && credential.ExpiresUtc.Value <= DateTime.UtcNow) return null;

            User user = await _Users.ReadById(credential.UserId, token).ConfigureAwait(false);
            if (user == null || !user.Active) return null;

            Tenant tenant = await _Tenants.ReadById(credential.TenantId, token).ConfigureAwait(false);
            if (tenant == null || !tenant.Active) return null;

            return new CallerContext
            {
                IsAuthenticated = true,
                PrincipalType = PrincipalType.Credential,
                PrincipalId = credential.Id,
                TenantId = credential.TenantId,
                UserId = user.Id,
                CredentialId = credential.Id,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                IsTenantAdmin = user.IsTenantAdmin
            };
        }

        private static CallerContext BuildUserCaller(User user, string sessionId)
        {
            return new CallerContext
            {
                IsAuthenticated = true,
                PrincipalType = PrincipalType.User,
                PrincipalId = user.Id,
                TenantId = user.TenantId,
                UserId = user.Id,
                SessionId = sessionId,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                IsTenantAdmin = user.IsTenantAdmin
            };
        }

        #endregion
    }
}
