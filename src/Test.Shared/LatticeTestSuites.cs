namespace Test.Shared
{
    using System.Collections.Generic;
    using Test.Shared.Suites;
    using Touchstone.Core;

    /// <summary>
    /// Central registry of every Touchstone test suite for the Lattice library. This is the single
    /// source of truth consumed by the CLI runner (Test.Automated) and by the xUnit and NUnit
    /// adapters (Test.Xunit / Test.Nunit).
    /// </summary>
    public static class LatticeTestSuites
    {
        /// <summary>
        /// All suites, in a sensible execution order.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    CollectionSuite.Build(),
                    DocumentSuite.Build(),
                    BatchSuite.Build(),
                    SearchSuite.Build(),
                    EnumerationSuite.Build(),
                    SchemaSuite.Build(),
                    IndexSuite.Build(),
                    IndexingModeSuite.Build(),
                    ConstraintsSuite.Build(),
                    EdgeCaseSuite.Build(),
                    FlushSuite.Build(),
                    ObjectLockingSuite.Build(),
                    RequestHistorySuite.Build(),
                    IntegrationSuite.Build(),
                };
            }
        }
    }
}
