namespace Lattice.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Full request history detail.
    /// </summary>
    public class RequestHistoryDetail : RequestHistoryEntry
    {
        /// <summary>
        /// Request headers captured for the request.
        /// </summary>
        public Dictionary<string, string> RequestHeaders
        {
            get => _RequestHeaders;
            set => _RequestHeaders = value ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Request body captured for the request.
        /// </summary>
        public string RequestBody { get; set; } = null;

        /// <summary>
        /// Response headers captured for the request.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders
        {
            get => _ResponseHeaders;
            set => _ResponseHeaders = value ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Response body captured for the request.
        /// </summary>
        public string ResponseBody { get; set; } = null;

        private Dictionary<string, string> _RequestHeaders = new Dictionary<string, string>();
        private Dictionary<string, string> _ResponseHeaders = new Dictionary<string, string>();

        /// <summary>
        /// Convert detail to entry metadata.
        /// </summary>
        /// <returns>Entry metadata.</returns>
        public RequestHistoryEntry ToEntry()
        {
            return new RequestHistoryEntry
            {
                Id = Id,
                CreatedUtc = CreatedUtc,
                CompletedUtc = CompletedUtc,
                RequestType = RequestType,
                Method = Method,
                Path = Path,
                Url = Url,
                SourceIp = SourceIp,
                CollectionId = CollectionId,
                DocumentId = DocumentId,
                SchemaId = SchemaId,
                TableName = TableName,
                StatusCode = StatusCode,
                Success = Success,
                ProcessingTimeMs = ProcessingTimeMs,
                RequestBodyLength = RequestBodyLength,
                ResponseBodyLength = ResponseBodyLength,
                RequestBodyTruncated = RequestBodyTruncated,
                ResponseBodyTruncated = ResponseBodyTruncated,
                RequestContentType = RequestContentType,
                ResponseContentType = ResponseContentType
            };
        }
    }
}
