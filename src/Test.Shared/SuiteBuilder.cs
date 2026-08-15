namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core;
    using Touchstone.Core;

    /// <summary>
    /// Fluent helper for assembling a <see cref="TestSuiteDescriptor"/> from a set of test bodies.
    ///
    /// The builder owns the per-test lifecycle: each case gets its own isolated temporary directory
    /// and a fresh <see cref="LatticeClient"/>, both of which are disposed/cleaned up automatically.
    /// This keeps individual test bodies focused purely on arrange/act/assert.
    /// </summary>
    public sealed class SuiteBuilder
    {
        #region Private-Members

        private readonly string _SuiteId;
        private readonly string _DisplayName;
        private readonly List<TestCaseDescriptor> _Cases = new List<TestCaseDescriptor>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate a suite builder.
        /// </summary>
        /// <param name="suiteId">Stable, unique suite identifier.</param>
        /// <param name="displayName">Human-readable suite name.</param>
        public SuiteBuilder(string suiteId, string displayName)
        {
            _SuiteId = suiteId ?? throw new ArgumentNullException(nameof(suiteId));
            _DisplayName = displayName ?? suiteId;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add a test whose body receives a fresh client. The temporary directory and client are
        /// created before and disposed after the body runs.
        /// </summary>
        /// <param name="displayName">Human-readable test name.</param>
        /// <param name="body">Test body.</param>
        /// <param name="tags">Optional tags for filtering.</param>
        /// <returns>This builder for chaining.</returns>
        public SuiteBuilder Add(string displayName, Func<LatticeClient, Task> body, params string[] tags)
        {
            return AddDir(displayName, (client, _) => body(client), tags);
        }

        /// <summary>
        /// Add a test whose body receives a fresh client and the owning temporary directory path.
        /// </summary>
        /// <param name="displayName">Human-readable test name.</param>
        /// <param name="body">Test body receiving the client and the temp directory path.</param>
        /// <param name="tags">Optional tags for filtering.</param>
        /// <returns>This builder for chaining.</returns>
        public SuiteBuilder AddDir(string displayName, Func<LatticeClient, string, Task> body, params string[] tags)
        {
            return AddDirSkip(displayName, false, null, body, tags);
        }

        /// <summary>
        /// Add a test whose body receives a fresh client and temp directory, optionally skipped.
        /// </summary>
        /// <param name="displayName">Human-readable test name.</param>
        /// <param name="skip">Whether to skip this test.</param>
        /// <param name="skipReason">Reason the test is skipped.</param>
        /// <param name="body">Test body receiving the client and the temp directory path.</param>
        /// <param name="tags">Optional tags for filtering.</param>
        /// <returns>This builder for chaining.</returns>
        public SuiteBuilder AddDirSkip(string displayName, bool skip, string skipReason, Func<LatticeClient, string, Task> body, params string[] tags)
        {
            Func<CancellationToken, Task> execute = async _ =>
            {
                string dir = LatticeTestContext.CreateTestDir();
                try
                {
                    using LatticeClient client = LatticeTestContext.CreateClient(dir);
                    await body(client, dir).ConfigureAwait(false);
                }
                finally
                {
                    LatticeTestContext.CleanupTestDir(dir);
                }
            };

            return AddRawSkip(displayName, skip, skipReason, execute, tags);
        }

        /// <summary>
        /// Add a raw test whose body manages its own resources. Use when a test needs multiple
        /// clients or a non-standard lifecycle.
        /// </summary>
        /// <param name="displayName">Human-readable test name.</param>
        /// <param name="execute">Raw execution delegate.</param>
        /// <param name="tags">Optional tags for filtering.</param>
        /// <returns>This builder for chaining.</returns>
        public SuiteBuilder AddRaw(string displayName, Func<CancellationToken, Task> execute, params string[] tags)
        {
            return AddRawSkip(displayName, false, null, execute, tags);
        }

        /// <summary>
        /// Add a raw test with explicit skip control.
        /// </summary>
        /// <param name="displayName">Human-readable test name.</param>
        /// <param name="skip">Whether to skip this test.</param>
        /// <param name="skipReason">Reason the test is skipped.</param>
        /// <param name="execute">Raw execution delegate.</param>
        /// <param name="tags">Optional tags for filtering.</param>
        /// <returns>This builder for chaining.</returns>
        public SuiteBuilder AddRawSkip(string displayName, bool skip, string skipReason, Func<CancellationToken, Task> execute, params string[] tags)
        {
            string caseId = _SuiteId + "_" + (_Cases.Count + 1).ToString("D3");
            _Cases.Add(new TestCaseDescriptor(
                _SuiteId,
                caseId,
                displayName,
                execute,
                (tags != null && tags.Length > 0) ? tags : null,
                skip,
                skipReason));
            return this;
        }

        /// <summary>
        /// Materialize the accumulated cases into a suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public TestSuiteDescriptor Build()
        {
            return new TestSuiteDescriptor(_SuiteId, _DisplayName, _Cases);
        }

        #endregion
    }
}
