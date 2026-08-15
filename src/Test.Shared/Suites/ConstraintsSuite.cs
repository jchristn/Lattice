namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lattice.Core;
    using Lattice.Core.Exceptions;
    using Lattice.Core.Models;
    using Lattice.Core.Validation;
    using Touchstone.Core;

    /// <summary>
    /// Tests for schema-enforcement and field-constraint validation across all enforcement modes and
    /// every constraint kind (type, nullability, regex, numeric range, length, allowed values, array
    /// element type, nested paths), covering both accepted and rejected documents.
    /// </summary>
    public static class ConstraintsSuite
    {
        private static FieldConstraint Constraint(
            string fieldPath,
            string dataType,
            bool required = false,
            bool nullable = true,
            string regex = null,
            decimal? minValue = null,
            decimal? maxValue = null,
            int? minLength = null,
            int? maxLength = null,
            List<string> allowedValues = null,
            string arrayElementType = null)
        {
            return new FieldConstraint
            {
                FieldPath = fieldPath,
                DataType = dataType,
                Required = required,
                Nullable = nullable,
                RegexPattern = regex,
                MinValue = minValue,
                MaxValue = maxValue,
                MinLength = minLength,
                MaxLength = maxLength,
                AllowedValues = allowedValues,
                ArrayElementType = arrayElementType
            };
        }

        private static Task<Collection> CollWith(LatticeClient client, SchemaEnforcementMode mode, params FieldConstraint[] constraints)
        {
            return client.Collection.Create(
                "Constrained",
                schemaEnforcementMode: mode,
                fieldConstraints: constraints.ToList());
        }

        private static async Task ExpectError(Func<Task> action, string expectedCode)
        {
            SchemaValidationException ex = await TestAssert.ThrowsAsync<SchemaValidationException>(action);
            List<string> codes = ex.Errors != null ? ex.Errors.Select(e => e.ErrorCode).ToList() : new List<string>();
            TestAssert.True(codes.Contains(expectedCode), $"Expected error code {expectedCode}; got [{string.Join(", ", codes)}]");
        }

        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("constraints", "Schema Constraints");

            // ----- Setup / configuration -----

            b.Add("Create with strict mode and constraints", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Strict,
                    Constraint("Name", "string", required: true),
                    Constraint("Age", "integer"));
                TestAssert.Equal(SchemaEnforcementMode.Strict, c.SchemaEnforcementMode);
                List<FieldConstraint> constraints = await client.Collection.GetConstraints(c.Id);
                TestAssert.Count(2, constraints);
            });

            b.Add("Valid document passes validation", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible,
                    Constraint("Name", "string", required: true));
                Document doc = await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                TestAssert.NotNull(doc, "Valid document should ingest");
            });

            b.Add("UpdateConstraints replaces constraint set", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                TestAssert.Count(0, await client.Collection.GetConstraints(c.Id));
                await client.Collection.UpdateConstraints(c.Id, SchemaEnforcementMode.Flexible,
                    new List<FieldConstraint> { Constraint("Email", "string") });
                TestAssert.Count(1, await client.Collection.GetConstraints(c.Id));
            });

            // ----- Required / missing -----

            b.Add("Missing required field fails", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible,
                    Constraint("Name", "string", required: true));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Other"":""x""}"), "MISSING_REQUIRED_FIELD");
            });

            // ----- Type validation -----

            b.Add("Type: string accepts string, rejects number", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("X", "string"));
                await client.Document.Ingest(c.Id, @"{""X"":""hello""}");
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""X"":5}"), "TYPE_MISMATCH");
            });

            b.Add("Type: integer accepts int, rejects decimal", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("X", "integer"));
                await client.Document.Ingest(c.Id, @"{""X"":42}");
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""X"":3.14}"), "TYPE_MISMATCH");
            });

            b.Add("Type: number accepts int and decimal", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("X", "number"));
                await client.Document.Ingest(c.Id, @"{""X"":42}");
                await client.Document.Ingest(c.Id, @"{""X"":3.14}");
            });

            b.Add("Type: boolean accepts bool, rejects string", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("X", "boolean"));
                await client.Document.Ingest(c.Id, @"{""X"":true}");
                await client.Document.Ingest(c.Id, @"{""X"":false}");
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""X"":""true""}"), "TYPE_MISMATCH");
            });

            b.Add("Type: array accepts array, rejects string", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("X", "array"));
                await client.Document.Ingest(c.Id, @"{""X"":[1,2,3]}");
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""X"":""notarray""}"), "TYPE_MISMATCH");
            });

            // ----- Nullability -----

            b.Add("Nullable field accepts null", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("X", "string", nullable: true));
                await client.Document.Ingest(c.Id, @"{""X"":null}");
            });

            b.Add("Non-nullable field rejects null", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("X", "string", nullable: false));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""X"":null}"), "NULL_NOT_ALLOWED");
            });

            // ----- Regex -----

            b.Add("Regex match succeeds", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Code", "string", regex: @"^[A-Z]{3}-\d{4}$"));
                await client.Document.Ingest(c.Id, @"{""Code"":""ABC-1234""}");
            });

            b.Add("Regex mismatch fails", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Code", "string", regex: @"^[A-Z]{3}-\d{4}$"));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Code"":""invalid""}"), "PATTERN_MISMATCH");
            });

            b.Add("Email regex validation", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Email", "string", regex: @"^[^@\s]+@[^@\s]+\.[^@\s]+$"));
                await client.Document.Ingest(c.Id, @"{""Email"":""joel@example.com""}");
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Email"":""not-an-email""}"), "PATTERN_MISMATCH");
            });

            // ----- Numeric range -----

            b.Add("MinValue succeeds", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Age", "integer", minValue: 18m));
                await client.Document.Ingest(c.Id, @"{""Age"":21}");
            });

            b.Add("MinValue fails", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Age", "integer", minValue: 18m));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Age"":16}"), "VALUE_TOO_SMALL");
            });

            b.Add("MaxValue succeeds", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Age", "integer", maxValue: 100m));
                await client.Document.Ingest(c.Id, @"{""Age"":75}");
            });

            b.Add("MaxValue fails", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Age", "integer", maxValue: 100m));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Age"":150}"), "VALUE_TOO_LARGE");
            });

            // ----- String length -----

            b.Add("String MinLength succeeds", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Password", "string", minLength: 8));
                await client.Document.Ingest(c.Id, @"{""Password"":""password123""}");
            });

            b.Add("String MinLength fails", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Password", "string", minLength: 8));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Password"":""short""}"), "STRING_TOO_SHORT");
            });

            b.Add("String MaxLength succeeds", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Name", "string", maxLength: 20));
                await client.Document.Ingest(c.Id, @"{""Name"":""joel""}");
            });

            b.Add("String MaxLength fails", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Name", "string", maxLength: 10));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Name"":""this name is way too long""}"), "STRING_TOO_LONG");
            });

            // ----- Array length -----

            b.Add("Array MinLength succeeds", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Tags", "array", minLength: 2));
                await client.Document.Ingest(c.Id, @"{""Tags"":[""a"",""b"",""c""]}");
            });

            b.Add("Array MaxLength fails", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Tags", "array", maxLength: 3));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Tags"":[""a"",""b"",""c"",""d"",""e""]}"), "ARRAY_TOO_LONG");
            });

            // ----- Allowed values -----

            b.Add("AllowedValues succeeds", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible,
                    Constraint("Status", "string", allowedValues: new List<string> { "active", "inactive" }));
                await client.Document.Ingest(c.Id, @"{""Status"":""active""}");
            });

            b.Add("AllowedValues fails", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible,
                    Constraint("Status", "string", allowedValues: new List<string> { "active", "inactive" }));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Status"":""deleted""}"), "VALUE_NOT_ALLOWED");
            });

            // ----- Array element type -----

            b.Add("Array element type succeeds", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Scores", "array", arrayElementType: "integer"));
                await client.Document.Ingest(c.Id, @"{""Scores"":[90,85,80]}");
            });

            b.Add("Array element type fails", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Scores", "array", arrayElementType: "integer"));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Scores"":[90,""x"",80]}"), "INVALID_ARRAY_ELEMENT");
            });

            // ----- Enforcement modes -----

            b.Add("Strict mode rejects extra fields", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Strict, Constraint("Name", "string", required: true));
                await ExpectError(() => client.Document.Ingest(c.Id, @"{""Name"":""x"",""Extra"":""y""}"), "UNEXPECTED_FIELD");
            });

            b.Add("Flexible mode allows extra fields", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Name", "string", required: true));
                await client.Document.Ingest(c.Id, @"{""Name"":""x"",""Extra"":""y""}");
            });

            b.Add("Partial mode validates only present specified fields", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Partial, Constraint("Age", "integer"));
                await client.Document.Ingest(c.Id, @"{""Name"":""x""}");
            });

            b.Add("None mode skips validation", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.None, Constraint("Age", "integer", required: true));
                await client.Document.Ingest(c.Id, @"{""Whatever"":""value""}");
            });

            // ----- Nested constraints -----

            b.Add("Nested field validation", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Address.City", "string"));
                await client.Document.Ingest(c.Id, @"{""Address"":{""City"":""Austin""}}");
            });

            b.Add("Deeply nested field validation", async client =>
            {
                Collection c = await CollWith(client, SchemaEnforcementMode.Flexible, Constraint("Person.Contact.Address.ZipCode", "string"));
                await client.Document.Ingest(c.Id, @"{""Person"":{""Contact"":{""Address"":{""ZipCode"":""78701""}}}}");
            });

            // ----- Direct validator -----

            b.Add("SchemaValidator: direct valid result", async client =>
            {
                SchemaValidator v = new SchemaValidator();
                ValidationResult r = v.Validate(@"{""Name"":""Joel""}", SchemaEnforcementMode.Flexible,
                    new List<FieldConstraint> { Constraint("Name", "string", required: true) });
                TestAssert.True(r.IsValid, "Expected valid result");
                TestAssert.Count(0, r.Errors);
            });

            b.Add("SchemaValidator: direct invalid result", async client =>
            {
                SchemaValidator v = new SchemaValidator();
                ValidationResult r = v.Validate(@"{""Age"":""notanumber""}", SchemaEnforcementMode.Flexible,
                    new List<FieldConstraint> { Constraint("Age", "integer") });
                TestAssert.False(r.IsValid, "Expected invalid result");
                TestAssert.True(r.Errors.Any(e => e.ErrorCode == "TYPE_MISMATCH"), "Expected TYPE_MISMATCH error");
            });

            return b.Build();
        }
    }
}
