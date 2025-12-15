# Lattice SDK - Development Guidelines

## Code Style Requirements

### No `var` Keyword

Always use explicit type declarations instead of `var`. This improves code readability and makes the type system more explicit.

**Do:**
```csharp
List<Document> documents = new List<Document>();
Dictionary<string, string> tags = new Dictionary<string, string>();
SearchResult result = await client.Search.Search(query);
Stopwatch stopwatch = Stopwatch.StartNew();
```

**Don't:**
```csharp
var documents = new List<Document>();
var tags = new Dictionary<string, string>();
var result = await client.Search.Search(query);
var stopwatch = Stopwatch.StartNew();
```

This applies to all contexts including:
- Variable declarations
- `foreach` loops: `foreach (Document doc in documents)` not `foreach (var doc in documents)`
- `using` statements: `using JsonDocument doc = JsonDocument.Parse(json)` not `using var doc = ...`
- LINQ results: `IEnumerable<IGrouping<string, Document>> groups = items.GroupBy(x => x.Id)`

### No Tuples

Do not use C# tuples for return types or variables. Instead, create proper structs or classes that clearly express the data being returned.

**Do:**
```csharp
// Define a struct for test outcomes
private struct TestOutcome
{
    public bool Success;
    public string Error;

    public static TestOutcome Pass() => new TestOutcome { Success = true, Error = null };
    public static TestOutcome Fail(string error) => new TestOutcome { Success = false, Error = error };
}

// Use the struct
private static async Task<TestOutcome> RunSomeTest()
{
    if (condition)
        return TestOutcome.Pass();
    else
        return TestOutcome.Fail("Error message");
}
```

**Don't:**
```csharp
// Don't use tuples
private static async Task<(bool success, string error)> RunSomeTest()
{
    if (condition)
        return (true, null);
    else
        return (false, "Error message");
}
```

### General Style

- Use explicit types in all declarations
- Prefer readability over brevity
- Create named types (structs/classes) when returning multiple values
- Use meaningful names that describe the purpose of the data

## Project Structure

- `src/Lattice.Core` - Core SDK library
- `src/Test.Automated` - Automated integration tests
- `src/Test.Throughput` - Performance/throughput tests

## Building and Testing

Build the solution:
```
dotnet build
```

Run automated tests:
```
dotnet run --project src/Test.Automated
```

Run throughput tests:
```
dotnet run --project src/Test.Throughput
```
