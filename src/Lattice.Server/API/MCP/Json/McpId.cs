namespace Lattice.Server.API.MCP.Json
{
    /// <summary>
    /// A JSON-RPC request identifier. The protocol allows the id to be a string, a number, or null; the
    /// value is echoed back verbatim on the matching response with its original type preserved.
    /// </summary>
    public class McpId
    {
        /// <summary>The string form of the id, when the request id was a JSON string.</summary>
        public string StringValue { get; set; } = null;

        /// <summary>The numeric form of the id, when the request id was a JSON number.</summary>
        public long? NumberValue { get; set; } = null;
    }
}
