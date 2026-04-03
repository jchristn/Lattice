namespace Lattice.Core.Repositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// SQLite implementation of request history methods.
    /// </summary>
    internal class RequestHistoryMethods : IRequestHistoryMethods
    {
        private const string EntryColumns = "id, createdutc, completedutc, requesttype, method, path, url, sourceip, collectionid, documentid, schemaid, tablename, statuscode, success, processingtimems, requestbodylength, responsebodylength, requestbodytruncated, responsebodytruncated, requestcontenttype, responsecontenttype";
        private const string DetailColumns = EntryColumns + ", requestheadersjson, requestbody, responseheadersjson, responsebody";

        private readonly SqliteRepository _Repo;

        internal RequestHistoryMethods(SqliteRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<RequestHistoryDetail> Create(RequestHistoryDetail detail, CancellationToken token = default)
        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO requesthistory (
                    id, createdutc, completedutc, requesttype, method, path, url, sourceip, collectionid, documentid, schemaid, tablename,
                    statuscode, success, processingtimems, requestbodylength, responsebodylength, requestbodytruncated, responsebodytruncated,
                    requestcontenttype, responsecontenttype, requestheadersjson, requestbody, responseheadersjson, responsebody
                )
                VALUES (
                    '{Sanitizer.Sanitize(detail.Id)}',
                    '{Converters.ToTimestamp(detail.CreatedUtc)}',
                    '{Converters.ToTimestamp(detail.CompletedUtc)}',
                    '{Sanitizer.Sanitize(detail.RequestType ?? "unknown")}',
                    '{Sanitizer.Sanitize(detail.Method ?? "GET")}',
                    '{Sanitizer.Sanitize(detail.Path ?? "/")}',
                    '{Sanitizer.Sanitize(detail.Url ?? "/")}',
                    '{Sanitizer.Sanitize(detail.SourceIp ?? "unknown")}',
                    {ToNullableString(detail.CollectionId)},
                    {ToNullableString(detail.DocumentId)},
                    {ToNullableString(detail.SchemaId)},
                    {ToNullableString(detail.TableName)},
                    {detail.StatusCode},
                    {ToBoolean(detail.Success)},
                    {detail.ProcessingTimeMs.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                    {detail.RequestBodyLength},
                    {detail.ResponseBodyLength},
                    {ToBoolean(detail.RequestBodyTruncated)},
                    {ToBoolean(detail.ResponseBodyTruncated)},
                    {ToNullableString(detail.RequestContentType)},
                    {ToNullableString(detail.ResponseContentType)},
                    {ToNullableString(SerializeDictionary(detail.RequestHeaders))},
                    {ToNullableString(detail.RequestBody)},
                    {ToNullableString(SerializeDictionary(detail.ResponseHeaders))},
                    {ToNullableString(detail.ResponseBody)}
                );
            ";

            await _Repo.ExecuteNonQueryAsync(query, token);
            return await ReadDetailById(detail.Id, token);
        }

        public async Task<RequestHistoryEntry> ReadEntryById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT {EntryColumns} FROM requesthistory WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.RequestHistoryEntryFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<RequestHistoryDetail> ReadDetailById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT {DetailColumns} FROM requesthistory WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.RequestHistoryDetailFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<RequestHistorySearchResult> Search(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();
            token.ThrowIfCancellationRequested();

            int page = filter.Page < 1 ? 1 : filter.Page;
            int pageSize = filter.PageSize < 1 ? 25 : (filter.PageSize > 250 ? 250 : filter.PageSize);
            int offset = (page - 1) * pageSize;
            string whereClause = BuildWhereClause(filter);

            DataTable countResult = await _Repo.ExecuteQueryAsync(
                $"SELECT COUNT(*) AS cnt FROM requesthistory {whereClause};",
                false,
                token);

            long totalCount = countResult.Rows.Count > 0 ? Convert.ToInt64(countResult.Rows[0]["cnt"]) : 0;

            string query = $@"
                SELECT {EntryColumns}
                FROM requesthistory
                {whereClause}
                ORDER BY createdutc DESC, completedutc DESC
                LIMIT {pageSize} OFFSET {offset};
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            List<RequestHistoryEntry> entries = new List<RequestHistoryEntry>();

            foreach (DataRow row in result.Rows)
            {
                RequestHistoryEntry entry = Converters.RequestHistoryEntryFromDataRow(row);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return new RequestHistorySearchResult
            {
                Data = entries,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<RequestHistorySummaryResult> GetSummary(RequestHistorySearchFilter filter, string interval, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();
            token.ThrowIfCancellationRequested();

            string normalizedInterval = NormalizeInterval(interval);
            string whereClause = BuildWhereClause(filter);
            string bucketExpression = GetBucketExpression(normalizedInterval);

            string query = $@"
                SELECT
                    {bucketExpression} AS bucketutc,
                    SUM(CASE WHEN statuscode < 400 THEN 1 ELSE 0 END) AS successcount,
                    SUM(CASE WHEN statuscode >= 400 THEN 1 ELSE 0 END) AS failurecount
                FROM requesthistory
                {whereClause}
                GROUP BY {bucketExpression}
                ORDER BY {bucketExpression};
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            List<RequestHistorySummaryBucket> buckets = new List<RequestHistorySummaryBucket>();

            foreach (DataRow row in result.Rows)
            {
                buckets.Add(new RequestHistorySummaryBucket
                {
                    TimestampUtc = DateTime.SpecifyKind(DateTime.Parse(row["bucketutc"].ToString()), DateTimeKind.Utc),
                    SuccessCount = Convert.ToInt64(row["successcount"]),
                    FailureCount = Convert.ToInt64(row["failurecount"])
                });
            }

            long totalSuccess = 0;
            long totalFailure = 0;
            foreach (RequestHistorySummaryBucket bucket in buckets)
            {
                totalSuccess += bucket.SuccessCount;
                totalFailure += bucket.FailureCount;
            }

            return new RequestHistorySummaryResult
            {
                Data = buckets,
                StartUtc = filter.StartUtc?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(-1),
                EndUtc = filter.EndUtc?.ToUniversalTime() ?? DateTime.UtcNow,
                Interval = normalizedInterval,
                TotalSuccess = totalSuccess,
                TotalFailure = totalFailure
            };
        }

        public async Task<bool> Delete(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            int rows = await _Repo.ExecuteNonQueryAsync(
                $"DELETE FROM requesthistory WHERE id = '{Sanitizer.Sanitize(id)}';",
                token);

            return rows > 0;
        }

        public async Task<long> DeleteBulk(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM requesthistory {BuildWhereClause(filter)};";
            return await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task<long> DeleteOlderThan(DateTime cutoffUtc, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM requesthistory WHERE createdutc < '{Converters.ToTimestamp(cutoffUtc.ToUniversalTime())}';";
            return await _Repo.ExecuteNonQueryAsync(query, token);
        }

        private static string BuildWhereClause(RequestHistorySearchFilter filter)
        {
            List<string> clauses = new List<string> { "1 = 1" };

            if (!string.IsNullOrWhiteSpace(filter.RequestType))
            {
                clauses.Add($"LOWER(requesttype) = '{Sanitizer.Sanitize(filter.RequestType.ToLowerInvariant())}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.Method))
            {
                clauses.Add($"UPPER(method) = '{Sanitizer.Sanitize(filter.Method.ToUpperInvariant())}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.PathContains))
            {
                clauses.Add($"LOWER(path) LIKE '%{Sanitizer.Sanitize(filter.PathContains.ToLowerInvariant())}%'");
            }

            if (!string.IsNullOrWhiteSpace(filter.CollectionId))
            {
                clauses.Add($"collectionid = '{Sanitizer.Sanitize(filter.CollectionId)}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.DocumentId))
            {
                clauses.Add($"documentid = '{Sanitizer.Sanitize(filter.DocumentId)}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.SchemaId))
            {
                clauses.Add($"schemaid = '{Sanitizer.Sanitize(filter.SchemaId)}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.TableName))
            {
                clauses.Add($"tablename = '{Sanitizer.Sanitize(filter.TableName)}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.SourceIp))
            {
                clauses.Add($"sourceip = '{Sanitizer.Sanitize(filter.SourceIp)}'");
            }

            if (filter.StatusCode.HasValue)
            {
                clauses.Add($"statuscode = {filter.StatusCode.Value}");
            }

            if (filter.Success.HasValue)
            {
                clauses.Add($"success = {ToBoolean(filter.Success.Value)}");
            }

            if (filter.StartUtc.HasValue)
            {
                clauses.Add($"createdutc >= '{Converters.ToTimestamp(filter.StartUtc.Value.ToUniversalTime())}'");
            }

            if (filter.EndUtc.HasValue)
            {
                clauses.Add($"createdutc <= '{Converters.ToTimestamp(filter.EndUtc.Value.ToUniversalTime())}'");
            }

            return "WHERE " + string.Join(" AND ", clauses);
        }

        private static string NormalizeInterval(string interval)
        {
            if (string.IsNullOrWhiteSpace(interval))
            {
                return "hour";
            }

            return interval.Trim().ToLowerInvariant() switch
            {
                "minute" => "minute",
                "15minute" => "15minute",
                "hour" => "hour",
                "6hour" => "6hour",
                "day" => "day",
                _ => "hour"
            };
        }

        private static string GetBucketExpression(string interval)
        {
            return interval switch
            {
                "minute" => "strftime('%Y-%m-%d %H:%M:00', createdutc)",
                "15minute" => "strftime('%Y-%m-%d %H:', createdutc) || printf('%02d:00', (CAST(strftime('%M', createdutc) AS INTEGER) / 15) * 15)",
                "hour" => "strftime('%Y-%m-%d %H:00:00', createdutc)",
                "6hour" => "strftime('%Y-%m-%d ', createdutc) || printf('%02d:00:00', (CAST(strftime('%H', createdutc) AS INTEGER) / 6) * 6)",
                "day" => "strftime('%Y-%m-%d 00:00:00', createdutc)",
                _ => "strftime('%Y-%m-%d %H:00:00', createdutc)"
            };
        }

        private static string ToNullableString(string value)
        {
            return value == null ? "NULL" : $"'{Sanitizer.Sanitize(value)}'";
        }

        private static int ToBoolean(bool value)
        {
            return value ? 1 : 0;
        }

        private static string SerializeDictionary(Dictionary<string, string> value)
        {
            return value == null ? null : JsonSerializer.Serialize(value);
        }
    }
}
