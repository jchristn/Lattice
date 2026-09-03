namespace Lattice.Server.API.MCP.Json
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A JSON-RPC 2.0 error response. Sent as HTTP 200 with the request id echoed and an error object.
    /// </summary>
    public class McpErrorResponse
    {
        /// <summary>The JSON-RPC protocol version.</summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>The echoed request id.</summary>
        [JsonConverter(typeof(McpIdConverter))]
        public McpId Id { get; set; } = new McpId();

        /// <summary>The error object.</summary>
        public McpError Error { get; set; } = null;

        /// <summary>Instantiate an empty error response.</summary>
        public McpErrorResponse()
        {
        }

        /// <summary>Instantiate an error response for a request id, code, and message.</summary>
        /// <param name="id">The request id to echo.</param>
        /// <param name="code">The JSON-RPC error code.</param>
        /// <param name="message">The error message.</param>
        public McpErrorResponse(McpId id, int code, string message)
        {
            Id = id ?? new McpId();
            Error = new McpError { Code = code, Message = message };
        }
    }
}
