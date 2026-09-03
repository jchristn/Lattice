namespace Lattice.Server.API.MCP.Json
{
    /// <summary>
    /// The <c>params</c> object of an MCP JSON-RPC request. For <c>tools/call</c> the relevant fields are
    /// <see cref="Name"/> and <see cref="Arguments"/>; <see cref="ProtocolVersion"/> arrives on
    /// <c>initialize</c>.
    /// </summary>
    public class McpRequestParams
    {
        /// <summary>The tool name, for <c>tools/call</c>.</summary>
        public string Name { get; set; } = null;

        /// <summary>The tool arguments, for <c>tools/call</c>.</summary>
        public McpToolArguments Arguments { get; set; } = null;

        /// <summary>The protocol version, for <c>initialize</c>.</summary>
        public string ProtocolVersion { get; set; } = null;
    }
}
