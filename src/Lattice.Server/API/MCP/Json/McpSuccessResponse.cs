namespace Lattice.Server.API.MCP.Json
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A JSON-RPC 2.0 success response. Sent as HTTP 200 with the request id echoed and a result payload.
    /// </summary>
    public class McpSuccessResponse
    {
        /// <summary>The JSON-RPC protocol version.</summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>The echoed request id.</summary>
        [JsonConverter(typeof(McpIdConverter))]
        public McpId Id { get; set; } = new McpId();

        /// <summary>The result payload.</summary>
        public object Result { get; set; } = null;

        /// <summary>Instantiate an empty response.</summary>
        public McpSuccessResponse()
        {
        }

        /// <summary>Instantiate a response for a request id and result.</summary>
        /// <param name="id">The request id to echo.</param>
        /// <param name="result">The result payload.</param>
        public McpSuccessResponse(McpId id, object result)
        {
            Id = id ?? new McpId();
            Result = result;
        }
    }
}
