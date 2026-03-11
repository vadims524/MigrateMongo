using System.Text.Json;
using MigrateMongo;
using Xunit;

namespace MigrateMongo.Tests.Unit;

// ConfigManager uses static state; Reset() is called in constructor and Dispose()
// so tests within this class remain isolated (xUnit runs them sequentially).
[Trait(TestCategories.Category, TestCategories.Unit)]
public sealed class ConfigManagerTests : IDisposable
{
    public ConfigManagerTests() => ConfigManager.Reset();
    public void Dispose() => ConfigManager.Reset();

    [Fact]
    public void WhenSetCalledWithNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ConfigManager.Set(null!));
    }

    [Fact]
    public async Task WhenSetCalledThenReadAsyncReturnsOverriddenConfig()
    {
        var config = MakeConfig();

        ConfigManager.Set(config);
        var result = await ConfigManager.ReadAsync();

        Assert.Equal(config, result);
    }

    [Fact]
    public async Task WhenSetCalledThenReadAsyncIgnoresAnyFilePath()
    {
        var config = MakeConfig();
        ConfigManager.Set(config);

        // A non-existent path should be ignored because the override takes precedence.
        var result = await ConfigManager.ReadAsync("/does/not/exist.json");

        Assert.Equal(config, result);
    }

    [Fact]
    public async Task WhenResetCalledAfterSetThenReadAsyncFallsBackToFile()
    {
        ConfigManager.Set(MakeConfig());
        ConfigManager.Reset();

        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.json");
        await Assert.ThrowsAsync<FileNotFoundException>(() => ConfigManager.ReadAsync(nonExistentPath));
    }

    [Fact]
    public async Task WhenConfigFileExistsThenReadAsyncDeserializesCorrectly()
    {
        var dir = CreateTempDir();
        await ConfigManager.WriteDefaultAsync(dir);
        var filePath = Path.Combine(dir, "mongo-migrate-config.json");

        var result = await ConfigManager.ReadAsync(filePath);

        Assert.Equal("mongodb://localhost:27017", result.MongoDB.Url);
        Assert.Equal("YOURDATABASENAME", result.MongoDB.DatabaseName);
        Assert.Equal("migrations", result.MigrationsDir);
        Assert.Equal("changelog", result.ChangelogCollectionName);
        Assert.Equal("changelog_lock", result.LockCollectionName);
        Assert.Equal(".cs", result.MigrationFileExtension);
        Assert.False(result.UseFileHash);
    }

    [Fact]
    public async Task WhenConfigFileDoesNotExistThenReadAsyncThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.json");

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => ConfigManager.ReadAsync(missingPath));

        Assert.Contains("Config file not found", ex.Message);
    }

    [Fact]
    public async Task WhenWriteDefaultCalledThenCreatesWellFormedJsonFile()
    {
        var dir = CreateTempDir();

        await ConfigManager.WriteDefaultAsync(dir);

        var filePath = Path.Combine(dir, "mongo-migrate-config.json");
        Assert.True(File.Exists(filePath));

        // Verify the file is valid JSON with expected shape
        var json = await File.ReadAllTextAsync(filePath);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("mongoDB", out _));
    }

    private static MigrateMongoConfig MakeConfig() => new()
    {
        MongoDB = new MongoDbSettings { Url = "mongodb://localhost:27017", DatabaseName = "testdb" }
    };

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }
}
