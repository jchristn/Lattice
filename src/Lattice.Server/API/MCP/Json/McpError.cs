namespace Lattice.Server.API.MCP.Json
{
    /// <summary>
    /// A JSON-RPC 2.0 error object (code and message).
    /// </summary>
    public class McpError
    {
        /// <summary>The JSON-RPC error code.</summary>
        public int Code { get; set; } = 0;

        /// <summary>The error message.</summary>
        public string Message { get; set; } = null;
    }
}
