using System.Reflection;
using MigrateMongo.Tests.Fakes;
using MigrateMongo.Tests.Helpers;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

using Api = global::MigrateMongo.MigrateMongo;

namespace MigrateMongo.Tests.Unit.Actions;

[Trait(TestCategories.Category, TestCategories.Unit)]
public sealed class UpActionTests : IDisposable
{
    private static readonly Assembly s_fakeAssembly =
        typeof(Migration_20210101000001_First).Assembly;

    private static readonly MigrateMongoConfig s_config = new()
    {
        MongoDB = new MongoDbSettings { Url = "mongodb://localhost:27017", DatabaseName = "testdb" }
    };

    public UpActionTests() => ConfigManager.Set(s_config);
    public void Dispose() => ConfigManager.Reset();

    [Fact]
    public async Task WhenChangelogEmptyThenAllMigrationsApplied()
    {
        var mocks = MongoMockHelper.Build();

        var result = await Api.UpAsync(mocks.Db, mocks.Client, s_fakeAssembly);

        Assert.Equal(2, result.Count);
        Assert.Contains("20210101000001-First", result);
        Assert.Contains("20210101000002-Second", result);
        await mocks.Changelog.Received(2).InsertOneAsync(
            Arg.Any<ChangelogEntry>(),
            Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenAllAlreadyAppliedThenNothingApplied()
    {
        var now = DateTime.UtcNow;
        var entries = new[]
        {
            new ChangelogEntry { FileName = "20210101000001-First", AppliedAt = now },
            new ChangelogEntry { FileName = "20210101000002-Second", AppliedAt = now }
        };
        var mocks = MongoMockHelper.Build(entries);

        var result = await Api.UpAsync(mocks.Db, mocks.Client, s_fakeAssembly);

        Assert.Empty(result);
        await mocks.Changelog.DidNotReceive().InsertOneAsync(
            Arg.Any<ChangelogEntry>(),
            Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenFirstAlreadyAppliedThenOnlySecondApplied()
    {
        var entries = new[]
        {
            new ChangelogEntry { FileName = "20210101000001-First", AppliedAt = DateTime.UtcNow }
        };
        var mocks = MongoMockHelper.Build(entries);

        var result = await Api.UpAsync(mocks.Db, mocks.Client, s_fakeAssembly);

        Assert.Single(result);
        Assert.Equal("20210101000002-Second", result[0]);
        await mocks.Changelog.Received(1).InsertOneAsync(
            Arg.Any<ChangelogEntry>(),
            Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>());
    }
}
