namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Aggregated request history summary bucket.
    /// </summary>
    public class RequestHistorySummaryBucket
    {
        /// <summary>
        /// Bucket start time in UTC.
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Successful request count.
        /// </summary>
        public long SuccessCount { get; set; } = 0;

        /// <summary>
        /// Failed request count.
        /// </summary>
        public long FailureCount { get; set; } = 0;

        /// <summary>
        /// Total request count.
        /// </summary>
        public long TotalCount => SuccessCount + FailureCount;
    }
}
