namespace Lattice.Core.Models
{
    /// <summary>
    /// Result of a lock acquisition attempt.
    /// </summary>
    public struct LockAcquisitionResult
    {
        #region Public-Members

        /// <summary>
        /// Whether the lock was successfully acquired.
        /// </summary>
        public bool Success;

        /// <summary>
        /// The acquired lock (if successful) or the existing lock (if blocked).
        /// </summary>
        public ObjectLock Lock;

        #endregion

        #region Public-Static-Methods

        /// <summary>
        /// Create a successful lock acquisition result.
        /// </summary>
        /// <param name="acquiredLock">The acquired lock.</param>
        /// <returns>Success result.</returns>
        public static LockAcquisitionResult Acquired(ObjectLock acquiredLock)
        {
            return new LockAcquisitionResult { Success = true, Lock = acquiredLock };
        }

        /// <summary>
        /// Create a blocked lock acquisition result.
        /// </summary>
        /// <param name="existingLock">The existing lock blocking acquisition.</param>
        /// <returns>Blocked result.</returns>
        public static LockAcquisitionResult Blocked(ObjectLock existingLock)
        {
            return new LockAcquisitionResult { Success = false, Lock = existingLock };
        }

        #endregion
    }
}
