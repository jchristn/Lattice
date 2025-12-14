namespace Lattice.Core.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents operation timing information.
    /// </summary>
    public class Timestamp
    {
        #region Public-Members

        /// <summary>
        /// Start time of the operation (UTC).
        /// </summary>
        [JsonPropertyName("start")]
        public DateTime Start { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// End time of the operation (UTC).
        /// </summary>
        [JsonPropertyName("end")]
        public DateTime End { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total elapsed time in milliseconds.
        /// </summary>
        [JsonPropertyName("totalMs")]
        public double TotalMs
        {
            get => (End - Start).TotalMilliseconds;
        }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Timestamp()
        {
        }

        /// <summary>
        /// Instantiate the object with a start time.
        /// </summary>
        /// <param name="start">Start time.</param>
        public Timestamp(DateTime start)
        {
            Start = start;
        }

        #endregion
    }
}
