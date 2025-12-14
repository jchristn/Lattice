namespace Lattice.Core.Flattening
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    /// <summary>
    /// Implementation of JSON document flattening.
    /// </summary>
    public class JsonFlattener : IJsonFlattener
    {
        #region Public-Methods

        /// <inheritdoc />
        public List<FlattenedValue> Flatten(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            List<FlattenedValue> result = new List<FlattenedValue>();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                FlattenRecursive(doc.RootElement, "", result, null);
            }

            return result;
        }

        #endregion

        #region Private-Methods

        private void FlattenRecursive(JsonElement element, string prefix, List<FlattenedValue> result, int? arrayPosition)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        FlattenRecursive(property.Value, key, result, null);
                    }
                    break;

                case JsonValueKind.Array:
                    int index = 0;
                    foreach (JsonElement arrayElement in element.EnumerateArray())
                    {
                        FlattenRecursive(arrayElement, prefix, result, index);
                        index++;
                    }
                    break;

                case JsonValueKind.String:
                    result.Add(new FlattenedValue(prefix, element.GetString(), "string", arrayPosition));
                    break;

                case JsonValueKind.Number:
                    string dataType = element.TryGetInt64(out _) ? "integer" : "number";
                    result.Add(new FlattenedValue(prefix, element.GetRawText(), dataType, arrayPosition));
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    result.Add(new FlattenedValue(prefix, element.GetBoolean().ToString().ToLower(), "boolean", arrayPosition));
                    break;

                case JsonValueKind.Null:
                    result.Add(new FlattenedValue(prefix, null, "null", arrayPosition));
                    break;
            }
        }

        #endregion
    }
}
