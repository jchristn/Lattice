namespace Lattice.Server.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Emit-side telemetry vocabulary for the Lattice server host: inbound HTTP and the request-history
    /// background worker. Depends only on the .NET base class library
    /// (<see cref="System.Diagnostics.Metrics.Meter"/>, <see cref="System.Diagnostics.ActivitySource"/>).
    /// A host subscribes to the meter and activity source named <c>Lattice.Server</c> to collect and
    /// export it. When nothing is listening every measurement is a cheap no-op.
    /// </summary>
    public static class ServerTelemetry
    {
        #region Public-Members

        /// <summary>
        /// The meter and activity source name a host subscribes to for server-host telemetry.
        /// </summary>
        public const string Name = "Lattice.Server";

        /// <summary>
        /// The server-host meter. Named <see cref="Name"/>.
        /// </summary>
        public static readonly Meter Meter = new Meter(Name, AssemblyVersion());

        /// <summary>
        /// The server-host activity source used for inbound HTTP server spans. Named <see cref="Name"/>.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new ActivitySource(Name, AssemblyVersion());

        #endregion

        #region Private-Members

        private static readonly Counter<long> _RequestCount = Meter.CreateCounter<long>(
            "http.server.request.count", "{request}", "Count of inbound HTTP requests by method and status.");

        private static readonly Histogram<double> _RequestDuration = Meter.CreateHistogram<double>(
            "http.server.request.duration", "s", "Duration of inbound HTTP server requests.");

        private static readonly UpDownCounter<long> _ActiveRequests = Meter.CreateUpDownCounter<long>(
            "http.server.active_requests", "{request}", "Number of in-flight HTTP requests.");

        private static readonly Histogram<long> _RequestBodySize = Meter.CreateHistogram<long>(
            "http.server.request.body.size", "By", "Size of inbound HTTP request bodies.");

        private static readonly Histogram<long> _ResponseBodySize = Meter.CreateHistogram<long>(
            "http.server.response.body.size", "By", "Size of outbound HTTP response bodies.");

        private static readonly Counter<long> _RequestHistoryRecorded = Meter.CreateCounter<long>(
            "lattice.requesthistory.recorded", "{record}", "Count of request-history records persisted by outcome.");

        private static readonly Counter<long> _RequestHistoryPruneRuns = Meter.CreateCounter<long>(
            "lattice.requesthistory.prune.runs", "{run}", "Count of request-history retention prune runs by outcome.");

        private static readonly Counter<long> _RequestHistoryPruned = Meter.CreateCounter<long>(
            "lattice.requesthistory.pruned", "{record}", "Count of request-history records deleted by retention pruning.");

        #endregion

        #region Public-Methods-Http

        /// <summary>
        /// Record that a request has begun (increments in-flight count). Pair with
        /// <see cref="RequestCompleted"/>.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        public static void RequestStarted(string method)
        {
            _ActiveRequests.Add(1, new KeyValuePair<string, object>("http.request.method", method ?? "UNKNOWN"));
        }

        /// <summary>
        /// Record a completed request: decrements in-flight count and records the request counter,
        /// duration histogram, and request/response body-size histograms.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="statusCode">HTTP response status code.</param>
        /// <param name="requestType">Coarse request classification (see <see cref="ClassifyRequestType"/>).</param>
        /// <param name="durationSeconds">Request duration in seconds.</param>
        /// <param name="requestBodyBytes">Request body size in bytes, or negative to skip.</param>
        /// <param name="responseBodyBytes">Response body size in bytes, or negative to skip.</param>
        public static void RequestCompleted(
            string method,
            int statusCode,
            string requestType,
            double durationSeconds,
            long requestBodyBytes,
            long responseBodyBytes)
        {
            string m = method ?? "UNKNOWN";
            string rt = requestType ?? "other";

            KeyValuePair<string, object> methodTag = new KeyValuePair<string, object>("http.request.method", m);
            KeyValuePair<string, object> statusTag = new KeyValuePair<string, object>("http.response.status_code", statusCode);
            KeyValuePair<string, object> typeTag = new KeyValuePair<string, object>("lattice.request_type", rt);

            _RequestCount.Add(1, methodTag, statusTag, typeTag);
            _RequestDuration.Record(durationSeconds, methodTag, statusTag, typeTag);

            if (requestBodyBytes >= 0)
                _RequestBodySize.Record(requestBodyBytes, methodTag, typeTag);
            if (responseBodyBytes >= 0)
                _ResponseBodySize.Record(responseBodyBytes, statusTag, typeTag);

            _ActiveRequests.Add(-1, methodTag);
        }

        /// <summary>
        /// Classify a request path into a coarse, low-cardinality request type for metric labels.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">Request path (no query string).</param>
        /// <returns>A coarse request type such as <c>health</c>, <c>collection</c>, <c>document</c>,
        /// <c>search</c>, <c>schema</c>, <c>table</c>, <c>requesthistory</c>, <c>swagger</c>, or
        /// <c>other</c>.</returns>
        public static string ClassifyRequestType(string method, string path)
        {
            if (string.IsNullOrEmpty(path)) return "other";
            string p = path.ToLowerInvariant();

            if (p == "/" || p.StartsWith("/v1.0/health")) return "health";
            if (p.StartsWith("/swagger") || p.Contains("openapi") || p.Contains("swagger")) return "swagger";
            if (p.StartsWith("/v1.0/requesthistory")) return "requesthistory";
            if (p.StartsWith("/v1.0/schemas")) return "schema";
            if (p.StartsWith("/v1.0/tables")) return "table";
            if (p.Contains("/documents/search")) return "search";
            if (p.Contains("/documents")) return "document";
            if (p.StartsWith("/v1.0/collections")) return "collection";
            return "other";
        }

        /// <summary>
        /// Start an inbound HTTP server span. The returned activity is null when nothing is sampling.
        /// The caller must set the final status code, set status, and dispose the activity.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">Request path.</param>
        /// <param name="requestType">Coarse request type.</param>
        /// <param name="collectionId">Collection identifier, or null.</param>
        /// <param name="documentId">Document identifier, or null.</param>
        /// <returns>The started server span, or null.</returns>
        public static Activity StartServerSpan(
            string method,
            string path,
            string requestType,
            string collectionId,
            string documentId)
        {
            string spanName = (method ?? "HTTP") + " " + (requestType ?? "request");
            Activity activity = ActivitySource.StartActivity(spanName, ActivityKind.Server);
            if (activity != null)
            {
                activity.SetTag("http.request.method", method);
                activity.SetTag("url.path", path);
                activity.SetTag("lattice.request_type", requestType);
                if (!string.IsNullOrEmpty(collectionId)) activity.SetTag("lattice.collection_id", collectionId);
                if (!string.IsNullOrEmpty(documentId)) activity.SetTag("lattice.document_id", documentId);
            }
            return activity;
        }

        #endregion

        #region Public-Methods-Worker

        /// <summary>
        /// Record the outcome of persisting a request-history record.
        /// </summary>
        /// <param name="outcome">Outcome: <c>ok</c> or <c>error</c>.</param>
        public static void RecordRequestHistoryRecorded(string outcome)
        {
            _RequestHistoryRecorded.Add(1, new KeyValuePair<string, object>("outcome", outcome ?? "ok"));
        }

        /// <summary>
        /// Record a request-history retention prune run and the number of records deleted.
        /// </summary>
        /// <param name="outcome">Outcome: <c>ok</c> or <c>error</c>.</param>
        /// <param name="deleted">Number of records deleted (0 or more).</param>
        public static void RecordRequestHistoryPrune(string outcome, long deleted)
        {
            _RequestHistoryPruneRuns.Add(1, new KeyValuePair<string, object>("outcome", outcome ?? "ok"));
            if (deleted > 0) _RequestHistoryPruned.Add(deleted);
        }

        #endregion

        #region Private-Methods

        private static string AssemblyVersion()
        {
            try
            {
                Version v = typeof(ServerTelemetry).Assembly.GetName().Version;
                return v == null ? "0.0.0" : v.ToString();
            }
            catch
            {
                return "0.0.0";
            }
        }

        #endregion
    }
}
