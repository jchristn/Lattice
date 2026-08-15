namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// NUnit host for the shared Lattice Touchstone suites.
    ///
    /// Every Touchstone test case from Test.Shared is projected into an individual NUnit test case via
    /// the Touchstone NUnit adapter (<see cref="TouchstoneTestCaseSource"/>), so each Lattice test
    /// shows up and reports independently under <c>dotnet test</c>.
    /// </summary>
    [TestFixture]
    public sealed class LatticeNunitTests
    {
        /// <summary>
        /// All non-skipped Lattice test cases, sourced from the shared suite registry.
        /// </summary>
        public static IEnumerable Cases()
        {
            return new TouchstoneTestCaseSource(LatticeTestSuites.All);
        }

        /// <summary>
        /// Execute a single Lattice test case.
        /// </summary>
        /// <param name="testCase">The shared test case descriptor.</param>
        [TestCaseSource(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
