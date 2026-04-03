namespace Lattice.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request history summary result.
    /// </summary>
    public class RequestHistorySummaryResult
    {
        /// <summary>
        /// Summary data buckets.
        /// </summary>
        public List<RequestHistorySummaryBucket> Data
        {
            get => _Data;
            set => _Data = value ?? new List<RequestHistorySummaryBucket>();
        }

        /// <summary>
        /// Summary start time.
        /// </summary>
        public DateTime StartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Summary end time.
        /// </summary>
        public DateTime EndUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Interval string.
        /// </summary>
        public string Interval { get; set; } = "hour";

        /// <summary>
        /// Total successful request count.
        /// </summary>
        public long TotalSuccess { get; set; } = 0;

        /// <summary>
        /// Total failed request count.
        /// </summary>
        public long TotalFailure { get; set; } = 0;

        /// <summary>
        /// Total request count.
        /// </summary>
        public long TotalRequests => TotalSuccess + TotalFailure;

        private List<RequestHistorySummaryBucket> _Data = new List<RequestHistorySummaryBucket>();
    }
}
