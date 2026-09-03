namespace Lattice.Server.API.MCP.Json
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// An inbound MCP JSON-RPC 2.0 request. Deserialized from the request body of <c>POST /v1.0/mcp</c>.
    /// </summary>
    public class McpRequest
    {
        /// <summary>The JSON-RPC protocol version (expected <c>2.0</c>).</summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = null;

        /// <summary>The request id, echoed on the response. Absent for notifications.</summary>
        [JsonConverter(typeof(McpIdConverter))]
        public McpId Id { get; set; } = null;

        /// <summary>The method name (<c>initialize</c>, <c>tools/list</c>, <c>tools/call</c>, and so on).</summary>
        public string Method { get; set; } = null;

        /// <summary>The method parameters.</summary>
        public McpRequestParams Params { get; set; } = null;
    }
}
