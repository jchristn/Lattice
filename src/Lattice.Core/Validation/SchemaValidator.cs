namespace Lattice.Core.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for schema validation.
    /// </summary>
    public interface ISchemaValidator
    {
        /// <summary>
        /// Validate a JSON document against field constraints.
        /// </summary>
        /// <param name="json">JSON document to validate.</param>
        /// <param name="mode">Schema enforcement mode.</param>
        /// <param name="fieldConstraints">Field constraints to validate against.</param>
        /// <returns>Validation result.</returns>
        ValidationResult Validate(string json, SchemaEnforcementMode mode, List<FieldConstraint> fieldConstraints);
    }

    /// <summary>
    /// Validates JSON documents against field constraints.
    /// </summary>
    public class SchemaValidator : ISchemaValidator
    {
        /// <summary>
        /// Validate a JSON document against field constraints.
        /// </summary>
        public ValidationResult Validate(string json, SchemaEnforcementMode mode, List<FieldConstraint> fieldConstraints)
        {
            if (mode == SchemaEnforcementMode.None)
                return ValidationResult.Success();

            if (string.IsNullOrWhiteSpace(json))
                return ValidationResult.Failure(new ValidationError
                {
                    FieldPath = "",
                    ErrorCode = ValidationErrorCodes.MissingRequiredField,
                    Message = "Document JSON cannot be empty"
                });

            if (fieldConstraints == null || fieldConstraints.Count == 0)
                return ValidationResult.Success();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                return ValidateDocument(doc.RootElement, mode, fieldConstraints);
            }
            catch (JsonException ex)
            {
                return ValidationResult.Failure(new ValidationError
                {
                    FieldPath = "",
                    ErrorCode = ValidationErrorCodes.TypeMismatch,
                    Message = $"Invalid JSON: {ex.Message}"
                });
            }
        }

        private ValidationResult ValidateDocument(JsonElement root, SchemaEnforcementMode mode, List<FieldConstraint> fieldConstraints)
        {
            var errors = new List<ValidationError>();
            var documentFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Extract all field paths from the document
            ExtractFieldPaths(root, "", documentFields);

            // Build constraint lookup
            var constraintsByPath = fieldConstraints.ToDictionary(c => c.FieldPath, StringComparer.OrdinalIgnoreCase);

            // Check required fields
            foreach (var constraint in fieldConstraints)
            {
                if (constraint.Required)
                {
                    bool fieldExists = documentFields.Any(f =>
                        f.Equals(constraint.FieldPath, StringComparison.OrdinalIgnoreCase) ||
                        f.StartsWith(constraint.FieldPath + ".", StringComparison.OrdinalIgnoreCase) ||
                        f.StartsWith(constraint.FieldPath + "[", StringComparison.OrdinalIgnoreCase));

                    if (!fieldExists)
                    {
                        // Also check if the field value is null
                        var value = GetValueAtPath(root, constraint.FieldPath);
                        if (value == null || (value.Value.ValueKind == JsonValueKind.Null && !constraint.Nullable))
                        {
                            errors.Add(new ValidationError
                            {
                                FieldPath = constraint.FieldPath,
                                ErrorCode = ValidationErrorCodes.MissingRequiredField,
                                Message = $"Required field '{constraint.FieldPath}' is missing"
                            });
                        }
                    }
                }
            }

            // Validate each field that has a constraint
            foreach (var fieldPath in documentFields)
            {
                // Find matching constraint (exact match or parent constraint for array elements)
                var constraint = FindMatchingConstraint(fieldPath, constraintsByPath);

                if (constraint != null)
                {
                    var value = GetValueAtPath(root, fieldPath);
                    if (value.HasValue)
                    {
                        var fieldErrors = ValidateField(fieldPath, value.Value, constraint);
                        errors.AddRange(fieldErrors);
                    }
                }
                else if (mode == SchemaEnforcementMode.Strict)
                {
                    // In strict mode, reject fields not in schema
                    // Skip array indices in path for this check
                    var basePath = GetBasePath(fieldPath);
                    if (!constraintsByPath.ContainsKey(basePath))
                    {
                        errors.Add(new ValidationError
                        {
                            FieldPath = fieldPath,
                            ErrorCode = ValidationErrorCodes.UnexpectedField,
                            Message = $"Unexpected field '{fieldPath}' not defined in schema"
                        });
                    }
                }
            }

            return errors.Count > 0 ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }

        private FieldConstraint FindMatchingConstraint(string fieldPath, Dictionary<string, FieldConstraint> constraintsByPath)
        {
            // Try exact match first
            if (constraintsByPath.TryGetValue(fieldPath, out var constraint))
                return constraint;

            // For array element paths like "items[0].name", try matching "items.name"
            var basePath = GetBasePath(fieldPath);
            if (basePath != fieldPath && constraintsByPath.TryGetValue(basePath, out constraint))
                return constraint;

            return null;
        }

        private string GetBasePath(string fieldPath)
        {
            // Convert "items[0].name" to "items.name"
            return Regex.Replace(fieldPath, @"\[\d+\]", "");
        }

        private void ExtractFieldPaths(JsonElement element, string currentPath, HashSet<string> paths)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        string newPath = string.IsNullOrEmpty(currentPath) ? property.Name : $"{currentPath}.{property.Name}";

                        if (property.Value.ValueKind == JsonValueKind.Object || property.Value.ValueKind == JsonValueKind.Array)
                        {
                            ExtractFieldPaths(property.Value, newPath, paths);
                        }
                        else
                        {
                            paths.Add(newPath);
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        string newPath = $"{currentPath}[{index}]";
                        if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
                        {
                            ExtractFieldPaths(item, newPath, paths);
                        }
                        else
                        {
                            paths.Add(newPath);
                        }
                        index++;
                    }
                    // Also add the array path itself for length validation
                    if (!string.IsNullOrEmpty(currentPath))
                        paths.Add(currentPath);
                    break;

                default:
                    if (!string.IsNullOrEmpty(currentPath))
                        paths.Add(currentPath);
                    break;
            }
        }

        private JsonElement? GetValueAtPath(JsonElement root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;

            var current = root;
            var parts = ParsePath(path);

            foreach (var part in parts)
            {
                if (part.IsArrayIndex)
                {
                    if (current.ValueKind != JsonValueKind.Array)
                        return null;

                    if (part.Index >= current.GetArrayLength())
                        return null;

                    current = current[part.Index];
                }
                else
                {
                    if (current.ValueKind != JsonValueKind.Object)
                        return null;

                    if (!current.TryGetProperty(part.Name, out var prop))
                        return null;

                    current = prop;
                }
            }

            return current;
        }

        private List<PathPart> ParsePath(string path)
        {
            var parts = new List<PathPart>();
            var regex = new Regex(@"([^\.\[\]]+)|\[(\d+)\]");
            var matches = regex.Matches(path);

            foreach (Match match in matches)
            {
                if (match.Groups[2].Success)
                {
                    parts.Add(new PathPart { IsArrayIndex = true, Index = int.Parse(match.Groups[2].Value) });
                }
                else if (match.Groups[1].Success)
                {
                    parts.Add(new PathPart { IsArrayIndex = false, Name = match.Groups[1].Value });
                }
            }

            return parts;
        }

        private List<ValidationError> ValidateField(string fieldPath, JsonElement value, FieldConstraint constraint)
        {
            var errors = new List<ValidationError>();

            // Check null
            if (value.ValueKind == JsonValueKind.Null)
            {
                if (!constraint.Nullable)
                {
                    errors.Add(new ValidationError
                    {
                        FieldPath = fieldPath,
                        ErrorCode = ValidationErrorCodes.NullNotAllowed,
                        Message = $"Field '{fieldPath}' cannot be null",
                        ActualValue = null
                    });
                }
                return errors;
            }

            // Type validation
            if (!string.IsNullOrEmpty(constraint.DataType))
            {
                var typeError = ValidateType(fieldPath, value, constraint.DataType);
                if (typeError != null)
                {
                    errors.Add(typeError);
                    return errors; // Skip other validations if type is wrong
                }
            }

            // String validations
            if (value.ValueKind == JsonValueKind.String)
            {
                string strValue = value.GetString();

                // Regex pattern
                if (!string.IsNullOrEmpty(constraint.RegexPattern))
                {
                    try
                    {
                        if (!Regex.IsMatch(strValue ?? "", constraint.RegexPattern))
                        {
                            errors.Add(new ValidationError
                            {
                                FieldPath = fieldPath,
                                ErrorCode = ValidationErrorCodes.PatternMismatch,
                                Message = $"Field '{fieldPath}' does not match pattern '{constraint.RegexPattern}'",
                                ActualValue = strValue,
                                ExpectedValue = constraint.RegexPattern
                            });
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        // Pattern timed out, skip validation
                    }
                }

                // Min/Max length
                if (constraint.MinLength.HasValue && (strValue?.Length ?? 0) < constraint.MinLength.Value)
                {
                    errors.Add(new ValidationError
                    {
                        FieldPath = fieldPath,
                        ErrorCode = ValidationErrorCodes.StringTooShort,
                        Message = $"Field '{fieldPath}' length {strValue?.Length ?? 0} is below minimum {constraint.MinLength}",
                        ActualValue = strValue?.Length ?? 0,
                        ExpectedValue = constraint.MinLength
                    });
                }

                if (constraint.MaxLength.HasValue && (strValue?.Length ?? 0) > constraint.MaxLength.Value)
                {
                    errors.Add(new ValidationError
                    {
                        FieldPath = fieldPath,
                        ErrorCode = ValidationErrorCodes.StringTooLong,
                        Message = $"Field '{fieldPath}' length {strValue?.Length ?? 0} exceeds maximum {constraint.MaxLength}",
                        ActualValue = strValue?.Length ?? 0,
                        ExpectedValue = constraint.MaxLength
                    });
                }

                // Allowed values
                if (constraint.AllowedValues != null && constraint.AllowedValues.Count > 0)
                {
                    if (!constraint.AllowedValues.Contains(strValue, StringComparer.Ordinal))
                    {
                        errors.Add(new ValidationError
                        {
                            FieldPath = fieldPath,
                            ErrorCode = ValidationErrorCodes.ValueNotAllowed,
                            Message = $"Field '{fieldPath}' value '{strValue}' is not in allowed values: [{string.Join(", ", constraint.AllowedValues)}]",
                            ActualValue = strValue,
                            ExpectedValue = constraint.AllowedValues
                        });
                    }
                }
            }

            // Number validations
            if (value.ValueKind == JsonValueKind.Number)
            {
                decimal numValue = value.GetDecimal();

                if (constraint.MinValue.HasValue && numValue < constraint.MinValue.Value)
                {
                    errors.Add(new ValidationError
                    {
                        FieldPath = fieldPath,
                        ErrorCode = ValidationErrorCodes.ValueTooSmall,
                        Message = $"Field '{fieldPath}' value {numValue} is below minimum {constraint.MinValue}",
                        ActualValue = numValue,
                        ExpectedValue = constraint.MinValue
                    });
                }

                if (constraint.MaxValue.HasValue && numValue > constraint.MaxValue.Value)
                {
                    errors.Add(new ValidationError
                    {
                        FieldPath = fieldPath,
                        ErrorCode = ValidationErrorCodes.ValueTooLarge,
                        Message = $"Field '{fieldPath}' value {numValue} exceeds maximum {constraint.MaxValue}",
                        ActualValue = numValue,
                        ExpectedValue = constraint.MaxValue
                    });
                }
            }

            // Array validations
            if (value.ValueKind == JsonValueKind.Array)
            {
                int length = value.GetArrayLength();

                if (constraint.MinLength.HasValue && length < constraint.MinLength.Value)
                {
                    errors.Add(new ValidationError
                    {
                        FieldPath = fieldPath,
                        ErrorCode = ValidationErrorCodes.ArrayTooShort,
                        Message = $"Array '{fieldPath}' length {length} is below minimum {constraint.MinLength}",
                        ActualValue = length,
                        ExpectedValue = constraint.MinLength
                    });
                }

                if (constraint.MaxLength.HasValue && length > constraint.MaxLength.Value)
                {
                    errors.Add(new ValidationError
                    {
                        FieldPath = fieldPath,
                        ErrorCode = ValidationErrorCodes.ArrayTooLong,
                        Message = $"Array '{fieldPath}' length {length} exceeds maximum {constraint.MaxLength}",
                        ActualValue = length,
                        ExpectedValue = constraint.MaxLength
                    });
                }

                // Array element type validation
                if (!string.IsNullOrEmpty(constraint.ArrayElementType))
                {
                    int idx = 0;
                    foreach (var item in value.EnumerateArray())
                    {
                        var elementError = ValidateType($"{fieldPath}[{idx}]", item, constraint.ArrayElementType);
                        if (elementError != null)
                        {
                            elementError.ErrorCode = ValidationErrorCodes.InvalidArrayElement;
                            errors.Add(elementError);
                        }
                        idx++;
                    }
                }
            }

            return errors;
        }

        private ValidationError ValidateType(string fieldPath, JsonElement value, string expectedType)
        {
            bool isValid = expectedType.ToLowerInvariant() switch
            {
                "string" => value.ValueKind == JsonValueKind.String,
                "integer" => value.ValueKind == JsonValueKind.Number && IsInteger(value),
                "number" => value.ValueKind == JsonValueKind.Number,
                "boolean" => value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False,
                "array" => value.ValueKind == JsonValueKind.Array,
                "object" => value.ValueKind == JsonValueKind.Object,
                _ => true // Unknown types pass
            };

            if (!isValid)
            {
                return new ValidationError
                {
                    FieldPath = fieldPath,
                    ErrorCode = ValidationErrorCodes.TypeMismatch,
                    Message = $"Field '{fieldPath}' expected type '{expectedType}' but got '{GetTypeName(value)}'",
                    ActualValue = GetTypeName(value),
                    ExpectedValue = expectedType
                };
            }

            return null;
        }

        private bool IsInteger(JsonElement value)
        {
            if (value.TryGetInt64(out _))
                return true;

            // Check if it's a decimal that equals its integer value
            decimal d = value.GetDecimal();
            return d == Math.Truncate(d);
        }

        private string GetTypeName(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => "string",
                JsonValueKind.Number when IsInteger(value) => "integer",
                JsonValueKind.Number => "number",
                JsonValueKind.True or JsonValueKind.False => "boolean",
                JsonValueKind.Array => "array",
                JsonValueKind.Object => "object",
                JsonValueKind.Null => "null",
                _ => "unknown"
            };
        }

        private class PathPart
        {
            public bool IsArrayIndex { get; set; }
            public string Name { get; set; }
            public int Index { get; set; }
        }
    }
}
