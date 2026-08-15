namespace Test.Shared.Suites
{
    using System;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;
    using Touchstone.Core;

    /// <summary>
    /// Tests for the request-history API: recording request/response detail and reading, searching,
    /// and deleting those records.
    /// </summary>
    public static class RequestHistorySuite
    {
        private static RequestHistoryDetail NewDetail(string collectionId = null)
        {
            DateTime now = DateTime.UtcNow;
            return new RequestHistoryDetail
            {
                CreatedUtc = now,
                CompletedUtc = now,
                RequestType = "document",
                Method = "POST",
                Path = "/v1/documents",
                Url = "/v1/documents",
                SourceIp = "127.0.0.1",
                CollectionId = collectionId,
                StatusCode = 201,
                Success = true,
                ProcessingTimeMs = 12.5,
                RequestContentType = "application/json",
                ResponseContentType = "application/json",
                RequestBody = @"{""hello"":""world""}",
                ResponseBody = @"{""id"":""doc_1""}"
            };
        }

        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("requesthistory", "Request History API");

            b.Add("Create then read entry by id", async client =>
            {
                RequestHistoryDetail created = await client.RequestHistory.Create(NewDetail());
                TestAssert.NotNull(created, "Create returned null");
                TestAssert.NotNullOrEmpty(created.Id, "Created entry has no id");

                RequestHistoryEntry entry = await client.RequestHistory.ReadEntryById(created.Id);
                TestAssert.NotNull(entry, "Entry not found by id");
                TestAssert.Equal("POST", entry.Method);
                TestAssert.Equal(201, entry.StatusCode);
            });

            b.Add("Create then read detail by id includes bodies", async client =>
            {
                RequestHistoryDetail created = await client.RequestHistory.Create(NewDetail());
                RequestHistoryDetail detail = await client.RequestHistory.ReadDetailById(created.Id);
                TestAssert.NotNull(detail, "Detail not found by id");
                TestAssert.Contains("hello", detail.RequestBody);
                TestAssert.Contains("doc_1", detail.ResponseBody);
            });

            b.Add("ReadEntryById: non-existent returns null", async client =>
            {
                RequestHistoryEntry entry = await client.RequestHistory.ReadEntryById("nonexistent");
                TestAssert.Null(entry, "Expected null for non-existent entry");
            });

            b.Add("Search returns recorded entries", async client =>
            {
                await client.RequestHistory.Create(NewDetail("col_search"));
                RequestHistorySearchResult result = await client.RequestHistory.Search(new RequestHistorySearchFilter
                {
                    Method = "POST"
                });
                TestAssert.NotNull(result, "Search returned null");
                TestAssert.True(result.Data.Count >= 1, "Expected at least one search result");
            });

            b.Add("Delete removes the entry", async client =>
            {
                RequestHistoryDetail created = await client.RequestHistory.Create(NewDetail());
                bool deleted = await client.RequestHistory.Delete(created.Id);
                TestAssert.True(deleted, "Delete should report success");
                RequestHistoryEntry entry = await client.RequestHistory.ReadEntryById(created.Id);
                TestAssert.Null(entry, "Entry should be gone after delete");
            });

            b.Add("DeleteOlderThan removes past entries", async client =>
            {
                await client.RequestHistory.Create(NewDetail());
                long removed = await client.RequestHistory.DeleteOlderThan(DateTime.UtcNow.AddMinutes(5));
                TestAssert.True(removed >= 1, "Expected at least one entry removed");
            });

            return b.Build();
        }
    }
}
