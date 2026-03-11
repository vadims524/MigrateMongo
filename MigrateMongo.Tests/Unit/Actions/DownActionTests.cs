using System.Reflection;
using MigrateMongo.Tests.Fakes;
using MigrateMongo.Tests.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

using Api = global::MigrateMongo.MigrateMongo;

namespace MigrateMongo.Tests.Unit.Actions;

[Trait(TestCategories.Category, TestCategories.Unit)]
public sealed class DownActionTests : IDisposable
{
    private static readonly Assembly s_fakeAssembly =
        typeof(Migration_20210101000001_First).Assembly;

    private static readonly MigrateMongoConfig s_config = new()
    {
        MongoDB = new MongoDbSettings { Url = "mongodb://localhost:27017", DatabaseName = "testdb" }
    };

    public DownActionTests() => ConfigManager.Set(s_config);
    public void Dispose() => ConfigManager.Reset();

    [Fact]
    public async Task WhenChangelogEmptyThenReturnsEmpty()
    {
        var mocks = MongoMockHelper.Build();

        var result = await Api.DownAsync(mocks.Db, mocks.Client, s_fakeAssembly);

        Assert.Empty(result);
        await mocks.Changelog.DidNotReceive().DeleteOneAsync(
            Arg.Any<FilterDefinition<ChangelogEntry>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenOneEntryThenItIsReverted()
    {
        var entries = new[]
        {
            new ChangelogEntry
            {
                Id = ObjectId.GenerateNewId(),
                FileName = "20210101000002-Second",
                AppliedAt = DateTime.UtcNow
            }
        };
        var mocks = MongoMockHelper.Build(entries);

        var result = await Api.DownAsync(mocks.Db, mocks.Client, s_fakeAssembly);

        Assert.Single(result);
        Assert.Equal("20210101000002-Second", result[0]);
        await mocks.Changelog.Received(1).DeleteOneAsync(
            Arg.Any<FilterDefinition<ChangelogEntry>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenTwoEntriesWithDifferentTimestampsThenOnlyLastReverted()
    {
        var entries = new[]
        {
            new ChangelogEntry
            {
                Id = ObjectId.GenerateNewId(),
                FileName = "20210101000001-First",
                AppliedAt = DateTime.UtcNow.AddHours(-1)
            },
            new ChangelogEntry
            {
                Id = ObjectId.GenerateNewId(),
                FileName = "20210101000002-Second",
                AppliedAt = DateTime.UtcNow
            }
        };
        var mocks = MongoMockHelper.Build(entries);

        var result = await Api.DownAsync(mocks.Db, mocks.Client, s_fakeAssembly);

        Assert.Single(result);
        Assert.Equal("20210101000002-Second", result[0]);
        await mocks.Changelog.Received(1).DeleteOneAsync(
            Arg.Any<FilterDefinition<ChangelogEntry>>(),
            Arg.Any<CancellationToken>());
    }
}
