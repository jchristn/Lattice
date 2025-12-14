namespace Lattice.Core.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;

    /// <summary>
    /// Implementation of schema generation from JSON documents.
    /// </summary>
    public class SchemaGenerator : ISchemaGenerator
    {
        #region Public-Methods

        /// <inheritdoc />
        public List<SchemaElement> ExtractElements(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            List<SchemaElement> elements = new List<SchemaElement>();
            int position = 0;

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                ExtractElementsRecursive(doc.RootElement, "", elements, ref position);
            }

            return elements;
        }

        /// <inheritdoc />
        public string ComputeSchemaHash(List<SchemaElement> elements)
        {
            if (elements == null || elements.Count == 0)
                return string.Empty;

            // Sort elements by key for consistent hashing
            List<SchemaElement> sortedElements = elements
                .OrderBy(e => e.Key)
                .ThenBy(e => e.DataType)
                .ToList();

            // Build a normalized string representation
            StringBuilder sb = new StringBuilder();
            foreach (SchemaElement element in sortedElements)
            {
                // Include key and type, but not nullable status for flexible matching
                sb.Append($"{element.Key}:{element.DataType};");
            }

            return HashHelper.ComputeSha256Hash(sb.ToString());
        }

        /// <inheritdoc />
        public bool SchemasMatch(List<SchemaElement> elementsA, List<SchemaElement> elementsB, bool allowFlexibility = true)
        {
            if (elementsA == null && elementsB == null) return true;
            if (elementsA == null || elementsB == null) return false;

            if (!allowFlexibility)
            {
                // Strict matching: must have same elements
                if (elementsA.Count != elementsB.Count) return false;

                List<string> keysA = elementsA.Select(e => $"{e.Key}:{e.DataType}").OrderBy(k => k).ToList();
                List<string> keysB = elementsB.Select(e => $"{e.Key}:{e.DataType}").OrderBy(k => k).ToList();

                return keysA.SequenceEqual(keysB);
            }

            // Flexible matching: one can have additional nullable fields
            Dictionary<string, SchemaElement> dictA = elementsA.ToDictionary(e => e.Key, e => e);
            Dictionary<string, SchemaElement> dictB = elementsB.ToDictionary(e => e.Key, e => e);

            // Get all unique keys
            List<string> allKeys = dictA.Keys.Union(dictB.Keys).ToList();

            foreach (string key in allKeys)
            {
                bool hasA = dictA.TryGetValue(key, out SchemaElement elementA);
                bool hasB = dictB.TryGetValue(key, out SchemaElement elementB);

                if (hasA && hasB)
                {
                    // Both have the key - types must be compatible
                    if (!TypesCompatible(elementA.DataType, elementB.DataType))
                        return false;
                }
                else if (hasA && !hasB)
                {
                    // A has key, B doesn't - only OK if A's element is nullable
                    if (!elementA.Nullable) return false;
                }
                else if (!hasA && hasB)
                {
                    // B has key, A doesn't - only OK if B's element is nullable
                    if (!elementB.Nullable) return false;
                }
            }

            return true;
        }

        #endregion

        #region Private-Methods

        private void ExtractElementsRecursive(JsonElement element, string prefix, List<SchemaElement> elements, ref int position)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        ExtractElementsRecursive(property.Value, key, elements, ref position);
                    }
                    break;

                case JsonValueKind.Array:
                    // For arrays, we record the array itself and analyze the first element for type
                    string arrayDataType = "array";
                    bool hasElements = element.GetArrayLength() > 0;

                    if (hasElements)
                    {
                        JsonElement firstElement = element[0];
                        arrayDataType = $"array<{GetDataType(firstElement)}>";
                    }

                    elements.Add(new SchemaElement
                    {
                        Key = prefix,
                        DataType = arrayDataType,
                        Nullable = true,
                        Position = position++
                    });

                    // If array contains objects, also extract their structure
                    if (hasElements && element[0].ValueKind == JsonValueKind.Object)
                    {
                        ExtractElementsRecursive(element[0], prefix, elements, ref position);
                    }
                    break;

                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    elements.Add(new SchemaElement
                    {
                        Key = prefix,
                        DataType = GetDataType(element),
                        Nullable = element.ValueKind == JsonValueKind.Null,
                        Position = position++
                    });
                    break;
            }
        }

        private string GetDataType(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => "string",
                JsonValueKind.Number => element.TryGetInt64(out _) ? "integer" : "number",
                JsonValueKind.True => "boolean",
                JsonValueKind.False => "boolean",
                JsonValueKind.Null => "null",
                JsonValueKind.Array => "array",
                JsonValueKind.Object => "object",
                _ => "unknown"
            };
        }

        private bool TypesCompatible(string typeA, string typeB)
        {
            if (typeA == typeB) return true;

            // null is compatible with any type
            if (typeA == "null" || typeB == "null") return true;

            // integer and number are compatible
            if ((typeA == "integer" && typeB == "number") ||
                (typeA == "number" && typeB == "integer"))
                return true;

            return false;
        }

        #endregion
    }
}
