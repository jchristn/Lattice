namespace Lattice.Core.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Emit-side telemetry vocabulary for the Lattice core engine. Following the "emit rides the BCL"
    /// pattern, this type depends only on <see cref="System.Diagnostics.Metrics.Meter"/> and
    /// <see cref="System.Diagnostics.ActivitySource"/> from the .NET base class library. It takes no
    /// dependency on any telemetry SDK. When nothing is listening every measurement is a cheap no-op;
    /// a host (see Lattice.Server) subscribes to the meter and activity source named
    /// <c>Lattice.Core</c> to collect and export it.
    /// <para>
    /// Instrument names, units, and label keys are the stable contract between this code and any
    /// observer. They follow OpenTelemetry semantic-convention style (dotted, lowercase).
    /// </para>
    /// </summary>
    public static class LatticeTelemetry
    {
        #region Public-Members

        /// <summary>
        /// The meter and activity source name a host subscribes to for core-engine telemetry.
        /// </summary>
        public const string Name = "Lattice.Core";

        /// <summary>
        /// The core-engine meter. Named <see cref="Name"/>.
        /// </summary>
        public static readonly Meter Meter = new Meter(Name, AssemblyVersion());

        /// <summary>
        /// The core-engine activity source used for operation spans. Named <see cref="Name"/>.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new ActivitySource(Name, AssemblyVersion());

        /// <summary>
        /// The database backend in use, stamped as the <c>db.system</c> label on operation metrics and
        /// spans (for example <c>sqlite</c>, <c>mysql</c>, <c>postgresql</c>, <c>sqlserver</c>). Set once
        /// at client initialization. Defaults to <c>unknown</c>.
        /// </summary>
        public static string DatabaseSystem { get; set; } = "unknown";

        #endregion

        #region Private-Members

        // Buckets tuned for sub-millisecond to multi-second operations (seconds).
        private static readonly double[] _DurationBucketsSeconds = new double[]
        {
            0.0005, 0.001, 0.0025, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30
        };

        private static readonly Counter<long> _Operations = Meter.CreateCounter<long>(
            "lattice.operations", "{operation}", "Count of core engine operations by outcome.");

        private static readonly Histogram<double> _OperationDuration = Meter.CreateHistogram<double>(
            "lattice.operation.duration", "s", "Duration of core engine operations.");

        private static readonly Counter<long> _DocumentsIngested = Meter.CreateCounter<long>(
            "lattice.documents.ingested", "{document}", "Count of documents ingested.");

        private static readonly Histogram<long> _IngestBatchSize = Meter.CreateHistogram<long>(
            "lattice.ingest.batch.size", "{document}", "Number of documents per batch ingest.");

        private static readonly Histogram<long> _SearchResults = Meter.CreateHistogram<long>(
            "lattice.search.results", "{document}", "Number of matching records returned by a search.");

        private static readonly Counter<long> _SchemasCreated = Meter.CreateCounter<long>(
            "lattice.schemas.created", "{schema}", "Count of new schemas discovered and created.");

        private static readonly Counter<long> _IndexTablesCreated = Meter.CreateCounter<long>(
            "lattice.index.tables.created", "{table}", "Count of index tables created during ingestion.");

        private static readonly Counter<long> _IndexRebuilds = Meter.CreateCounter<long>(
            "lattice.index.rebuilds", "{operation}", "Count of index rebuild operations by outcome.");

        private static readonly Counter<long> _LockContention = Meter.CreateCounter<long>(
            "lattice.lock.contention", "{event}", "Count of document lock contention events.");

        #endregion

        #region Public-Methods

        /// <summary>
        /// Begin an instrumented operation scope: starts an internal span on
        /// <see cref="ActivitySource"/> and, on disposal, records the operation counter and duration
        /// histogram tagged with the operation name, outcome, and database system. Wrap a method body
        /// with a <c>using</c> and call <see cref="OperationScope.Fail"/> in a catch to mark failures.
        /// </summary>
        /// <param name="operation">The operation name (for example <c>document.ingest</c>).</param>
        /// <param name="collection">Optional collection identifier tag for the span.</param>
        /// <returns>An operation scope; dispose it to record metrics and end the span.</returns>
        public static OperationScope StartOperation(string operation, string collection = null)
        {
            return new OperationScope(operation, collection);
        }

        /// <summary>
        /// Record that documents were ingested.
        /// </summary>
        /// <param name="collection">Collection identifier.</param>
        /// <param name="count">Number of documents.</param>
        /// <param name="mode">Ingest mode: <c>single</c> or <c>batch</c>.</param>
        public static void RecordDocumentsIngested(string collection, long count, string mode)
        {
            if (count <= 0) return;
            _DocumentsIngested.Add(count,
                new KeyValuePair<string, object>("collection", collection ?? "unknown"),
                new KeyValuePair<string, object>("mode", mode ?? "single"));
        }

        /// <summary>
        /// Record the size of a batch ingest.
        /// </summary>
        /// <param name="collection">Collection identifier.</param>
        /// <param name="size">Number of documents in the batch.</param>
        public static void RecordBatchSize(string collection, long size)
        {
            _IngestBatchSize.Record(size,
                new KeyValuePair<string, object>("collection", collection ?? "unknown"));
        }

        /// <summary>
        /// Record the number of records returned by a search.
        /// </summary>
        /// <param name="collection">Collection identifier.</param>
        /// <param name="count">Number of matching records.</param>
        public static void RecordSearchResults(string collection, long count)
        {
            _SearchResults.Record(count,
                new KeyValuePair<string, object>("collection", collection ?? "unknown"));
        }

        /// <summary>
        /// Record that a new schema was discovered and created.
        /// </summary>
        /// <param name="collection">Collection identifier.</param>
        public static void RecordSchemaCreated(string collection)
        {
            _SchemasCreated.Add(1, new KeyValuePair<string, object>("collection", collection ?? "unknown"));
            Activity.Current?.AddEvent(new ActivityEvent("schema.created"));
        }

        /// <summary>
        /// Record that an index table was created during ingestion.
        /// </summary>
        public static void RecordIndexTableCreated()
        {
            _IndexTablesCreated.Add(1);
            Activity.Current?.AddEvent(new ActivityEvent("index.table.created"));
        }

        /// <summary>
        /// Record the outcome of an index rebuild.
        /// </summary>
        /// <param name="collection">Collection identifier.</param>
        /// <param name="outcome">Outcome: <c>ok</c> or <c>error</c>.</param>
        public static void RecordIndexRebuild(string collection, string outcome)
        {
            _IndexRebuilds.Add(1,
                new KeyValuePair<string, object>("collection", collection ?? "unknown"),
                new KeyValuePair<string, object>("outcome", outcome ?? "ok"));
        }

        /// <summary>
        /// Record a document lock contention event (a lock could not be acquired).
        /// </summary>
        /// <param name="collection">Collection identifier.</param>
        public static void RecordLockContention(string collection)
        {
            _LockContention.Add(1, new KeyValuePair<string, object>("collection", collection ?? "unknown"));
            Activity.Current?.AddEvent(new ActivityEvent("lock.contended"));
        }

        /// <summary>
        /// Record an operation's counter and duration. Called by <see cref="OperationScope"/> on
        /// disposal; also usable directly for operations that do not open a span.
        /// </summary>
        /// <param name="operation">Operation name.</param>
        /// <param name="outcome">Outcome: <c>ok</c> or <c>error</c>.</param>
        /// <param name="seconds">Elapsed seconds.</param>
        public static void RecordOperation(string operation, string outcome, double seconds)
        {
            KeyValuePair<string, object> opTag = new KeyValuePair<string, object>("operation", operation ?? "unknown");
            KeyValuePair<string, object> outcomeTag = new KeyValuePair<string, object>("outcome", outcome ?? "ok");
            KeyValuePair<string, object> dbTag = new KeyValuePair<string, object>("db.system", DatabaseSystem);

            _Operations.Add(1, opTag, outcomeTag, dbTag);
            _OperationDuration.Record(seconds, opTag, outcomeTag, dbTag);
        }

        #endregion

        #region Private-Methods

        private static string AssemblyVersion()
        {
            try
            {
                Version v = typeof(LatticeTelemetry).Assembly.GetName().Version;
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
