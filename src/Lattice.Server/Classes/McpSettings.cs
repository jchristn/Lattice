namespace Lattice.Server.Classes
{
    /// <summary>
    /// Model Context Protocol (MCP) server settings. The MCP endpoint is hosted in-process on the main
    /// server, behind the same authentication and authorization as the REST API.
    /// </summary>
    public class McpSettings
    {
        /// <summary>
        /// Whether the MCP endpoint is enabled. Default true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// The path the MCP JSON-RPC endpoint is served at. Default <c>/v1.0/mcp</c>.
        /// </summary>
        public string Path { get; set; } = "/v1.0/mcp";

        /// <summary>
        /// The server name advertised in the MCP initialize handshake.
        /// </summary>
        public string ServerName { get; set; } = "lattice";
    }
}
