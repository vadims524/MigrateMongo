using System.Reflection;
using MigrateMongo.Tests.Fakes;
using MigrateMongo.Tests.Helpers;
using MongoDB.Driver;
using Xunit;

using Api = global::MigrateMongo.MigrateMongo;

namespace MigrateMongo.Tests.Unit.Actions;

[Trait(TestCategories.Category, TestCategories.Unit)]
public sealed class StatusActionTests : IDisposable
{
    private static readonly Assembly s_fakeAssembly =
        typeof(Migration_20210101000001_First).Assembly;

    private static readonly MigrateMongoConfig s_config = new()
    {
        MongoDB = new MongoDbSettings { Url = "mongodb://localhost:27017", DatabaseName = "testdb" }
    };

    public StatusActionTests() => ConfigManager.Set(s_config);
    public void Dispose() => ConfigManager.Reset();

    [Fact]
    public async Task WhenChangelogEmptyThenAllPending()
    {
        var mocks = MongoMockHelper.Build();

        var result = await Api.StatusAsync(mocks.Db, s_fakeAssembly);

        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal("PENDING", s.AppliedAt));
    }

    [Fact]
    public async Task WhenBothAppliedThenNonePending()
    {
        var now = DateTime.UtcNow;
        var entries = new[]
        {
            new ChangelogEntry { FileName = "20210101000001-First", AppliedAt = now },
            new ChangelogEntry { FileName = "20210101000002-Second", AppliedAt = now }
        };
        var mocks = MongoMockHelper.Build(entries);

        var result = await Api.StatusAsync(mocks.Db, s_fakeAssembly);

        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.NotEqual("PENDING", s.AppliedAt));
    }

    [Fact]
    public async Task WhenOnlyFirstAppliedThenSecondIsPending()
    {
        var entries = new[]
        {
            new ChangelogEntry { FileName = "20210101000001-First", AppliedAt = DateTime.UtcNow }
        };
        var mocks = MongoMockHelper.Build(entries);

        var result = await Api.StatusAsync(mocks.Db, s_fakeAssembly);

        Assert.Equal(2, result.Count);
        Assert.NotEqual("PENDING", result[0].AppliedAt);
        Assert.Equal("PENDING", result[1].AppliedAt);
    }
}
