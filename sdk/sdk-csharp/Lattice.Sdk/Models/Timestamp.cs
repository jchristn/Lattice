namespace Lattice.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents timing information for an enumeration operation.
    /// </summary>
    public class Timestamp
    {
        /// <summary>
        /// Gets or sets the UTC time at which the operation started.
        /// </summary>
        [JsonPropertyName("start")]
        public DateTime? Start { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the operation ended.
        /// </summary>
        [JsonPropertyName("end")]
        public DateTime? End { get; set; }

        /// <summary>
        /// Gets or sets the total elapsed time, in milliseconds.
        /// </summary>
        [JsonPropertyName("totalMs")]
        public double TotalMs { get; set; }
    }
}
