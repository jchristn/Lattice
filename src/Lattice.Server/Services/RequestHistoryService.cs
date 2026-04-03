namespace Lattice.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SyslogLogging;
    using Lattice.Core;
    using Lattice.Core.Models;
    using Lattice.Server.Classes;

    /// <summary>
    /// Database-backed request history service with automatic retention pruning.
    /// </summary>
    public class RequestHistoryService : IDisposable
    {
        private readonly LatticeClient _Client;
        private readonly RequestHistorySettings _Settings;
        private readonly LoggingModule _Logging;
        private readonly string _Header = "[RequestHistoryService] ";
        private CancellationTokenSource _RetentionCts;
        private Task _RetentionTask;
        private bool _Disposed = false;

        /// <summary>
        /// Instantiate the request history service.
        /// </summary>
        /// <param name="client">Lattice client.</param>
        /// <param name="settings">Request history settings.</param>
        /// <param name="logging">Logging module.</param>
        public RequestHistoryService(LatticeClient client, RequestHistorySettings settings, LoggingModule logging = null)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging;

            if (_Settings.Enabled)
            {
                _RetentionCts = new CancellationTokenSource();
                _RetentionTask = Task.Run(() => RunRetentionLoopAsync(_RetentionCts.Token));
                _Logging?.Info(_Header + "initialized database-backed storage with " + _Settings.RetentionDays + "-day retention");
            }
            else
            {
                _Logging?.Info(_Header + "disabled");
            }
        }

        /// <summary>
        /// Whether request history capture is enabled.
        /// </summary>
        public bool Enabled => _Settings.Enabled;

        /// <summary>
        /// Persist a request history detail entry.
        /// </summary>
        /// <param name="detail">Detail entry.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task RecordAsync(RequestHistoryDetail detail, CancellationToken token = default)
        {
            if (!Enabled || detail == null) return;

            try
            {
                SanitizeHeaders(detail);
                ApplyBodyLimits(detail);
                await _Client.RequestHistory.Create(detail, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging?.Warn(_Header + "Failed to record request history: " + e.Message);
            }
        }

        /// <summary>
        /// Retrieve a single request history entry by identifier.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Entry metadata, or null.</returns>
        public Task<RequestHistoryEntry> GetEntryAsync(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Task.FromResult<RequestHistoryEntry>(null);
            }

            return _Client.RequestHistory.ReadEntryById(id, token);
        }

        /// <summary>
        /// Retrieve full request history detail by identifier.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Full detail, or null.</returns>
        public Task<RequestHistoryDetail> GetDetailAsync(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Task.FromResult<RequestHistoryDetail>(null);
            }

            return _Client.RequestHistory.ReadDetailById(id, token);
        }

        /// <summary>
        /// Search request history entries using the supplied filter.
        /// </summary>
        /// <param name="filter">Search filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Paged search result.</returns>
        public Task<RequestHistorySearchResult> SearchAsync(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();
            return _Client.RequestHistory.Search(filter, token);
        }

        /// <summary>
        /// Get aggregated request history summary buckets.
        /// </summary>
        /// <param name="filter">Search filter.</param>
        /// <param name="interval">Bucket interval.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Summary result.</returns>
        public Task<RequestHistorySummaryResult> GetSummaryAsync(RequestHistorySearchFilter filter, string interval, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();

            DateTime startUtc = filter.StartUtc?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(-1);
            DateTime endUtc = filter.EndUtc?.ToUniversalTime() ?? DateTime.UtcNow;
            if (endUtc < startUtc)
            {
                (startUtc, endUtc) = (endUtc, startUtc);
            }

            RequestHistorySearchFilter effectiveFilter = new RequestHistorySearchFilter
            {
                RequestType = filter.RequestType,
                Method = filter.Method,
                PathContains = filter.PathContains,
                CollectionId = filter.CollectionId,
                DocumentId = filter.DocumentId,
                SchemaId = filter.SchemaId,
                TableName = filter.TableName,
                SourceIp = filter.SourceIp,
                StatusCode = filter.StatusCode,
                Success = filter.Success,
                StartUtc = startUtc,
                EndUtc = endUtc,
                Page = 1,
                PageSize = 1
            };

            string intervalLabel = NormalizeInterval(interval, endUtc - startUtc);
            return _Client.RequestHistory.GetSummary(effectiveFilter, intervalLabel, token);
        }

        /// <summary>
        /// Delete a single request history entry.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the entry was deleted.</returns>
        public Task<bool> DeleteAsync(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Task.FromResult(false);
            }

            return _Client.RequestHistory.Delete(id, token);
        }

        /// <summary>
        /// Delete all request history entries matching the supplied filter.
        /// </summary>
        /// <param name="filter">Delete filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Deleted entry count.</returns>
        public Task<long> DeleteBulkAsync(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();
            return _Client.RequestHistory.DeleteBulk(filter, token);
        }

        /// <summary>
        /// Dispose of background resources.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;

            if (_RetentionCts != null)
            {
                try
                {
                    _RetentionCts.Cancel();
                    if (_RetentionTask != null)
                    {
                        _RetentionTask.GetAwaiter().GetResult();
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (AggregateException e) when (e.InnerException is OperationCanceledException)
                {
                }
                finally
                {
                    _RetentionCts.Dispose();
                }
            }

            _Disposed = true;
        }

        private async Task RunRetentionLoopAsync(CancellationToken token)
        {
            try
            {
                await PruneExpiredEntriesAsync(token).ConfigureAwait(false);

                using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(_Settings.PruneIntervalMinutes));
                while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                {
                    await PruneExpiredEntriesAsync(token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                _Logging?.Warn(_Header + "Request history retention loop stopped unexpectedly: " + e.Message);
            }
        }

        private async Task PruneExpiredEntriesAsync(CancellationToken token)
        {
            DateTime cutoffUtc = DateTime.UtcNow.AddDays(-_Settings.RetentionDays);
            long deleted = await _Client.RequestHistory.DeleteOlderThan(cutoffUtc, token).ConfigureAwait(false);

            if (deleted > 0)
            {
                _Logging?.Info(_Header + "pruned " + deleted + " request history record(s) older than " + cutoffUtc.ToString("O"));
            }
        }

        private void SanitizeHeaders(RequestHistoryDetail detail)
        {
            detail.RequestHeaders = SanitizeHeaderDictionary(detail.RequestHeaders);
            detail.ResponseHeaders = SanitizeHeaderDictionary(detail.ResponseHeaders);
        }

        private static Dictionary<string, string> SanitizeHeaderDictionary(Dictionary<string, string> headers)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null) return ret;

            foreach (KeyValuePair<string, string> header in headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key)) continue;
                if (IsSensitiveHeader(header.Key))
                {
                    ret[header.Key] = "***redacted***";
                }
                else
                {
                    ret[header.Key] = header.Value ?? string.Empty;
                }
            }

            return ret;
        }

        private static bool IsSensitiveHeader(string key)
        {
            return key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)
                || key.Equals("X-Api-Key", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Api-Key", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyBodyLimits(RequestHistoryDetail detail)
        {
            detail.RequestBodyLength = GetUtf8Length(detail.RequestBody);
            detail.ResponseBodyLength = GetUtf8Length(detail.ResponseBody);

            if (!string.IsNullOrEmpty(detail.RequestBody))
            {
                detail.RequestBody = TruncateUtf8(detail.RequestBody, _Settings.MaxRequestBodyBytes, out bool truncated);
                detail.RequestBodyTruncated = truncated;
            }

            if (!string.IsNullOrEmpty(detail.ResponseBody))
            {
                detail.ResponseBody = TruncateUtf8(detail.ResponseBody, _Settings.MaxResponseBodyBytes, out bool truncated);
                detail.ResponseBodyTruncated = truncated;
            }
        }

        private static long GetUtf8Length(string value)
        {
            return string.IsNullOrEmpty(value) ? 0 : Encoding.UTF8.GetByteCount(value);
        }

        private static string TruncateUtf8(string value, int maxBytes, out bool truncated)
        {
            truncated = false;
            if (string.IsNullOrEmpty(value)) return value;

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length <= maxBytes) return value;

            truncated = true;
            int length = maxBytes;
            while (length > 0 && (bytes[length - 1] & 0xC0) == 0x80)
            {
                length--;
            }

            return Encoding.UTF8.GetString(bytes, 0, length);
        }

        private static string NormalizeInterval(string interval, TimeSpan range)
        {
            if (!string.IsNullOrWhiteSpace(interval))
            {
                string normalized = interval.Trim().ToLowerInvariant();
                if (normalized == "minute" || normalized == "15minute" || normalized == "hour" || normalized == "6hour" || normalized == "day")
                {
                    return normalized;
                }
            }

            if (range.TotalHours <= 2) return "minute";
            if (range.TotalHours <= 36) return "15minute";
            if (range.TotalDays <= 10) return "hour";
            if (range.TotalDays <= 45) return "6hour";
            return "day";
        }
    }
}
