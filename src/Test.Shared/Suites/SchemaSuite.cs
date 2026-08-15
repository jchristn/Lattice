namespace Test.Shared.Suites
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Touchstone.Core;

    /// <summary>
    /// Tests for the schema API: automatic schema generation on ingestion, schema reuse for identical
    /// structures, and schema element inspection.
    /// </summary>
    public static class SchemaSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("schema", "Schema API");

            b.Add("ReadAll: returns schemas after ingest", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                List<Schema> schemas = await client.Schema.ReadAll();
                TestAssert.True(schemas.Count >= 1, "Expected at least one schema");
            });

            b.Add("Schema reuse for identical structure", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                Document d1 = await client.Document.Ingest(c.Id, @"{""FirstName"":""A"",""LastName"":""B""}");
                Document d2 = await client.Document.Ingest(c.Id, @"{""FirstName"":""C"",""LastName"":""D""}");
                TestAssert.Equal(d1.SchemaId, d2.SchemaId, "Identical structures should share a schema");
            });

            b.Add("Distinct schema for different structure", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                Document d1 = await client.Document.Ingest(c.Id, @"{""FirstName"":""A""}");
                Document d2 = await client.Document.Ingest(c.Id, @"{""Age"":30,""City"":""X""}");
                TestAssert.NotEqual(d1.SchemaId, d2.SchemaId, "Different structures should have different schemas");
            });

            b.Add("ReadById: by document schema id", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                Schema s = await client.Schema.ReadById(doc.SchemaId);
                TestAssert.NotNull(s, "Schema not found by id");
                TestAssert.Equal(doc.SchemaId, s.Id);
            });

            b.Add("ReadById: non-existent returns null", async client =>
            {
                Schema s = await client.Schema.ReadById("sch_nonexistent");
                TestAssert.Null(s, "Expected null for non-existent schema");
            });

            b.Add("GetElements: returns elements for schema", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(c.Id, @"{""FirstName"":""Joel"",""LastName"":""Christner""}");
                List<SchemaElement> elems = await client.Schema.GetElements(doc.SchemaId);
                TestAssert.True(elems.Count >= 2, $"Expected at least 2 elements, got {elems.Count}");
            });

            b.Add("GetElements: keys present", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(c.Id, @"{""FirstName"":""Joel"",""LastName"":""Christner""}");
                List<SchemaElement> elems = await client.Schema.GetElements(doc.SchemaId);
                HashSet<string> keys = elems.Select(e => e.Key).ToHashSet();
                TestAssert.True(keys.Contains("FirstName") && keys.Contains("LastName"), "Expected keys missing");
            });

            b.Add("GetElements: data types inferred", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(c.Id, @"{""Name"":""Joel"",""Age"":40}");
                List<SchemaElement> elems = await client.Schema.GetElements(doc.SchemaId);
                SchemaElement name = elems.First(e => e.Key == "Name");
                SchemaElement age = elems.First(e => e.Key == "Age");
                TestAssert.Contains("string", name.DataType.ToLowerInvariant());
                string ageType = age.DataType.ToLowerInvariant();
                TestAssert.True(ageType.Contains("int") || ageType.Contains("number"), $"Unexpected Age type: {age.DataType}");
            });

            return b.Build();
        }
    }
}
