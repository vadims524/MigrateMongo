using MigrateMongo;
using Xunit;

// Use a type alias to resolve the MigrateMongo namespace vs MigrateMongo class ambiguity.
using Api = global::MigrateMongo.MigrateMongo;

namespace MigrateMongo.Tests.Unit.Actions;

[Trait(TestCategories.Category, TestCategories.Unit)]
public sealed class InitActionTests : IDisposable
{
    public InitActionTests() => ConfigManager.Reset();
    public void Dispose() => ConfigManager.Reset();

    [Fact]
    public async Task WhenInitCalledWithDirectoryThenCreatesConfigFile()
    {
        var dir = CreateTempDir();

        await Api.InitAsync(dir);

        Assert.True(File.Exists(Path.Combine(dir, "mongo-migrate-config.json")));
    }

    [Fact]
    public async Task WhenInitCalledWithDirectoryThenCreatesMigrationsSubdirectory()
    {
        var dir = CreateTempDir();

        await Api.InitAsync(dir);

        Assert.True(Directory.Exists(Path.Combine(dir, "migrations")));
    }

    [Fact]
    public async Task WhenInitCalledTwiceThenDoesNotThrow()
    {
        var dir = CreateTempDir();

        await Api.InitAsync(dir);
        var ex = await Record.ExceptionAsync(() => Api.InitAsync(dir));

        Assert.Null(ex);
    }

    [Fact]
    public async Task WhenInitCalledThenConfigFileContainsDefaultMongoUrl()
    {
        var dir = CreateTempDir();
        await Api.InitAsync(dir);

        var filePath = Path.Combine(dir, "mongo-migrate-config.json");
        var config = await ConfigManager.ReadAsync(filePath);

        Assert.Equal("mongodb://localhost:27017", config.MongoDB.Url);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }
}
