using MigrateMongo;
using Xunit;

using Api = global::MigrateMongo.MigrateMongo;

namespace MigrateMongo.Tests.Unit.Actions;

[Trait(TestCategories.Category, TestCategories.Unit)]
public sealed class CreateActionTests : IDisposable
{
    private readonly string _migrationsDir = CreateTempDir();

    private static readonly MigrateMongoConfig s_config = new()
    {
        MongoDB = new MongoDbSettings { Url = "mongodb://localhost:27017", DatabaseName = "testdb" }
    };

    public CreateActionTests() => ConfigManager.Set(s_config);
    public void Dispose() => ConfigManager.Reset();

    [Fact]
    public async Task WhenCreateCalledThenCreatesFileInMigrationsDir()
    {
        var fileName = await Api.CreateAsync("add_index", _migrationsDir);

        Assert.True(File.Exists(Path.Combine(_migrationsDir, fileName)));
    }

    [Fact]
    public async Task WhenCreateCalledThenFileNameHas14DigitTimestampPrefix()
    {
        var fileName = await Api.CreateAsync("add_index", _migrationsDir);

        Assert.Matches(@"^\d{14}-add_index\.cs$", fileName);
    }

    [Fact]
    public async Task WhenDescriptionHasSpacesThenFileNameSanitizesThemToUnderscores()
    {
        var fileName = await Api.CreateAsync("add new index", _migrationsDir);

        Assert.Contains("add_new_index", fileName);
    }

    [Fact]
    public async Task WhenDescriptionIsEmptyThenCreateThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Api.CreateAsync("", _migrationsDir));
    }

    [Fact]
    public async Task WhenDescriptionIsWhitespaceThenCreateThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Api.CreateAsync("   ", _migrationsDir));
    }

    [Fact]
    public async Task WhenCreateCalledThenGeneratedFileContainsIMigrationInterface()
    {
        var fileName = await Api.CreateAsync("my_migration", _migrationsDir);
        var content = await File.ReadAllTextAsync(Path.Combine(_migrationsDir, fileName));

        Assert.Contains("IMigration", content);
    }

    [Fact]
    public async Task WhenCreateCalledThenGeneratedFileContainsMigrateMongoUsing()
    {
        var fileName = await Api.CreateAsync("my_migration", _migrationsDir);
        var content = await File.ReadAllTextAsync(Path.Combine(_migrationsDir, fileName));

        Assert.Contains("using MigrateMongo;", content);
    }

    [Fact]
    public async Task WhenCreateCalledThenGeneratedFileContainsUpAndDownMethods()
    {
        var fileName = await Api.CreateAsync("my_migration", _migrationsDir);
        var content = await File.ReadAllTextAsync(Path.Combine(_migrationsDir, fileName));

        Assert.Contains("UpAsync", content);
        Assert.Contains("DownAsync", content);
    }

    [Fact]
    public async Task WhenSampleMigrationFileExistsThenCreateUsesItAsTemplate()
    {
        const string customTemplate = "// custom template content";
        await File.WriteAllTextAsync(Path.Combine(_migrationsDir, "sample-migration.cs"), customTemplate);

        var fileName = await Api.CreateAsync("my_migration", _migrationsDir);
        var content = await File.ReadAllTextAsync(Path.Combine(_migrationsDir, fileName));

        Assert.Equal(customTemplate, content);
    }

    [Fact]
    public async Task WhenCreateCalledThenGeneratedClassNameContainsTimestampAndDescription()
    {
        var fileName = await Api.CreateAsync("add_users", _migrationsDir);
        var className = MigrationsLocator.FileNameToTypeName(Path.GetFileNameWithoutExtension(fileName));
        var content = await File.ReadAllTextAsync(Path.Combine(_migrationsDir, fileName));

        Assert.Contains(className, content);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }
}
