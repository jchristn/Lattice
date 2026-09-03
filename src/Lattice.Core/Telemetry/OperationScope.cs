namespace Lattice.Core.Telemetry
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// A disposable scope that times a core-engine operation, opens an internal span on
    /// <see cref="LatticeTelemetry.ActivitySource"/>, and on disposal records the operation counter and
    /// duration histogram and closes the span. Use with a <c>using</c> statement and call
    /// <see cref="Fail(Exception)"/> from a catch block to mark the operation as failed.
    /// <para>
    /// When no listener is sampling, the span is null and the scope is a cheap timing wrapper.
    /// </para>
    /// </summary>
    public sealed class OperationScope : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// The underlying activity (span), or null when nothing is sampling.
        /// </summary>
        public Activity Activity { get; }

        #endregion

        #region Private-Members

        private readonly string _Operation;
        private readonly long _StartTimestamp;
        private string _Outcome = "ok";
        private bool _Disposed;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Begin an operation scope.
        /// </summary>
        /// <param name="operation">The operation name (for example <c>document.ingest</c>).</param>
        /// <param name="collection">Optional collection identifier tag.</param>
        public OperationScope(string operation, string collection = null)
        {
            _Operation = String.IsNullOrWhiteSpace(operation) ? "unknown" : operation;
            _StartTimestamp = Stopwatch.GetTimestamp();

            Activity = LatticeTelemetry.ActivitySource.StartActivity(_Operation, ActivityKind.Internal);
            if (Activity != null)
            {
                Activity.SetTag("operation", _Operation);
                Activity.SetTag("db.system", LatticeTelemetry.DatabaseSystem);
                if (!String.IsNullOrEmpty(collection)) Activity.SetTag("lattice.collection", collection);
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add or overwrite a tag on the span.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <param name="value">Tag value.</param>
        /// <returns>This scope, for chaining.</returns>
        public OperationScope SetTag(string key, object value)
        {
            Activity?.SetTag(key, value);
            return this;
        }

        /// <summary>
        /// Mark this operation as failed and record the exception on the span.
        /// </summary>
        /// <param name="exception">The exception that caused the failure, or null.</param>
        public void Fail(Exception exception = null)
        {
            _Outcome = "error";
            if (Activity == null) return;

            Activity.SetStatus(ActivityStatusCode.Error, exception?.Message);
            if (exception != null)
            {
                ActivityTagsCollection tags = new ActivityTagsCollection
                {
                    { "exception.type", exception.GetType().FullName },
                    { "exception.message", exception.Message }
                };
                if (exception.StackTrace != null) tags["exception.stacktrace"] = exception.StackTrace;
                Activity.AddEvent(new ActivityEvent("exception", default, tags));
            }
        }

        /// <summary>
        /// Record metrics for the operation and end the span.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            double seconds = Stopwatch.GetElapsedTime(_StartTimestamp).TotalSeconds;

            if (Activity != null)
            {
                Activity.SetTag("outcome", _Outcome);
                if (_Outcome == "ok" && Activity.Status == ActivityStatusCode.Unset)
                {
                    Activity.SetStatus(ActivityStatusCode.Ok);
                }
                Activity.Dispose();
            }

            LatticeTelemetry.RecordOperation(_Operation, _Outcome, seconds);
        }

        #endregion
    }
}
