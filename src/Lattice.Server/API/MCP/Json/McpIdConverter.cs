namespace Lattice.Server.API.MCP.Json
{
    using System;
    using System.Globalization;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Reads and writes a <see cref="McpId"/> as a JSON string, number, or null, preserving the original
    /// type so a response echoes the request id exactly. Uses the streaming reader/writer rather than a
    /// DOM element.
    /// </summary>
    public class McpIdConverter : JsonConverter<McpId>
    {
        /// <summary>Read an id token into an <see cref="McpId"/>.</summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The target type.</param>
        /// <param name="options">Serializer options.</param>
        /// <returns>The parsed id (never null; a JSON null yields an empty id).</returns>
        public override McpId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return new McpId { StringValue = reader.GetString() };
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out long number)) return new McpId { NumberValue = number };
                    return new McpId { StringValue = reader.GetDouble().ToString(CultureInfo.InvariantCulture) };
                case JsonTokenType.Null:
                    return new McpId();
                default:
                    reader.Skip();
                    return new McpId();
            }
        }

        /// <summary>Write an <see cref="McpId"/> back as a string, number, or null.</summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The id to write.</param>
        /// <param name="options">Serializer options.</param>
        public override void Write(Utf8JsonWriter writer, McpId value, JsonSerializerOptions options)
        {
            if (value == null || (value.StringValue == null && value.NumberValue == null))
            {
                writer.WriteNullValue();
                return;
            }

            if (value.NumberValue.HasValue)
            {
                writer.WriteNumberValue(value.NumberValue.Value);
                return;
            }

            writer.WriteStringValue(value.StringValue);
        }
    }
}
