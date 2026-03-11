using Testcontainers.MongoDb;
using Xunit;

namespace MigrateMongo.Tests.Integration;

/// <summary>
/// Starts a single MongoDB container shared across all tests in the collection.
/// Each test gets its own isolated database via <see cref="UniqueDatabase"/>.
/// </summary>
public sealed class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Returns a unique database name to isolate each test.
    /// </summary>
    public static string UniqueDatabase() => $"testdb_{Guid.NewGuid():N}";
}
