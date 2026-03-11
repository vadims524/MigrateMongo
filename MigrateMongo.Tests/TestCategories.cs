namespace MigrateMongo.Tests;

/// <summary>
/// xUnit trait constants for categorising tests.
/// Use as: <c>[Trait(TestCategories.Category, TestCategories.Unit)]</c>
/// Filter as: <c>dotnet test --filter "Category=Unit"</c>
///            <c>dotnet test --filter "Category=Integration"</c>
/// </summary>
internal static class TestCategories
{
    internal const string Category = "Category";
    internal const string Unit = "Unit";
    internal const string Integration = "Integration";
}
