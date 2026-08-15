namespace Test.Xunit
{
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;
    using global::Xunit;

    /// <summary>
    /// xUnit host for the shared Lattice Touchstone suites.
    ///
    /// Every Touchstone test case from Test.Shared is projected into an individual xUnit theory case
    /// via the Touchstone xUnit adapter (<see cref="TouchstoneTheoryData"/>), so each Lattice test
    /// shows up and reports independently under <c>dotnet test</c>.
    /// </summary>
    public sealed class LatticeXunitTests
    {
        /// <summary>
        /// All non-skipped Lattice test cases, sourced from the shared suite registry.
        /// </summary>
        public static TheoryData<TestCaseDescriptor> Cases => new TouchstoneTheoryData(LatticeTestSuites.All);

        /// <summary>
        /// Execute a single Lattice test case.
        /// </summary>
        /// <param name="testCase">The shared test case descriptor.</param>
        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
