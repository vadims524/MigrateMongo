using MigrateMongo;
using MigrateMongo.Tests.Fakes;
using MongoDB.Driver;
using Xunit;

using Api = global::MigrateMongo.MigrateMongo;

namespace MigrateMongo.Tests.Integration;

/// <summary>
/// Integration tests for Up / Down / Status actions against a real MongoDB instance.
/// Each test gets a unique database so tests are fully isolated.
/// </summary>
[Collection("MongoDb")]
[Trait(TestCategories.Category, TestCategories.Integration)]
public sealed class UpDownStatusTests : IClassFixture<MongoDbFixture>, IAsyncLifetime
{
    private static readonly System.Reflection.Assembly s_fakeAssembly =
        typeof(Migration_20210101000001_First).Assembly;

    private readonly MongoDbFixture _fixture;
    private IMongoClient _client = null!;
    private IMongoDatabase _db = null!;

    public UpDownStatusTests(MongoDbFixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        var dbName = MongoDbFixture.UniqueDatabase();

        var config = new MigrateMongoConfig
        {
            MongoDB = new MongoDbSettings
            {
                Url = _fixture.ConnectionString,
                DatabaseName = dbName
            }
        };
        ConfigManager.Set(config);

        _client = new MongoClient(_fixture.ConnectionString);
        _db = _client.GetDatabase(dbName);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _client.DropDatabaseAsync(_db.DatabaseNamespace.DatabaseName);
        ConfigManager.Reset();
    }

    // ── Status ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenNoMigrationsRunThenStatusShowsAllPending()
    {
        var status = await Api.StatusAsync(_db, s_fakeAssembly);

        Assert.All(status, s => Assert.Equal("PENDING", s.AppliedAt));
    }

    [Fact]
    public async Task WhenNoMigrationsRunThenStatusContainsBothFakeMigrations()
    {
        var status = await Api.StatusAsync(_db, s_fakeAssembly);

        Assert.Contains(status, s => s.FileName == "20210101000001-First");
        Assert.Contains(status, s => s.FileName == "20210101000002-Second");
    }

    // ── Up ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenUpCalledThenReturnsAllMigratedFileNames()
    {
        var migrated = await Api.UpAsync(_db, _client, s_fakeAssembly);

        Assert.Contains("20210101000001-First", migrated);
        Assert.Contains("20210101000002-Second", migrated);
    }

    [Fact]
    public async Task WhenUpCalledThenStatusShowsAllApplied()
    {
        await Api.UpAsync(_db, _client, s_fakeAssembly);

        var status = await Api.StatusAsync(_db, s_fakeAssembly);

        Assert.All(status, s => Assert.NotEqual("PENDING", s.AppliedAt));
    }

    [Fact]
    public async Task WhenUpCalledTwiceThenSecondCallReturnsEmpty()
    {
        await Api.UpAsync(_db, _client, s_fakeAssembly);
        var secondRun = await Api.UpAsync(_db, _client, s_fakeAssembly);

        Assert.Empty(secondRun);
    }

    // ── Down ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenDownCalledAfterUpThenLastMigrationReverted()
    {
        await Api.UpAsync(_db, _client, s_fakeAssembly);

        var migratedDown = await Api.DownAsync(_db, _client, s_fakeAssembly);

        Assert.Single(migratedDown);
        Assert.Equal("20210101000002-Second", migratedDown[0]);
    }

    [Fact]
    public async Task WhenDownCalledAfterUpThenStatusShowsRevertedMigrationAsPending()
    {
        await Api.UpAsync(_db, _client, s_fakeAssembly);
        await Api.DownAsync(_db, _client, s_fakeAssembly);

        var status = await Api.StatusAsync(_db, s_fakeAssembly);
        var revertedStatus = status.Single(s => s.FileName == "20210101000002-Second");

        Assert.Equal("PENDING", revertedStatus.AppliedAt);
    }

    [Fact]
    public async Task WhenDownCalledOnEmptyChangelogThenReturnsEmpty()
    {
        var migratedDown = await Api.DownAsync(_db, _client, s_fakeAssembly);

        Assert.Empty(migratedDown);
    }

    // ── Up → Down → Up round-trip ─────────────────────────────────────────────

    [Fact]
    public async Task WhenUpDownUpCalledThenFinalStatusShowsAllApplied()
    {
        await Api.UpAsync(_db, _client, s_fakeAssembly);
        await Api.DownAsync(_db, _client, s_fakeAssembly);
        await Api.UpAsync(_db, _client, s_fakeAssembly);

        var status = await Api.StatusAsync(_db, s_fakeAssembly);

        Assert.All(status, s => Assert.NotEqual("PENDING", s.AppliedAt));
    }
}

[CollectionDefinition("MongoDb")]
public sealed class MongoDbCollectionDefinition : ICollectionFixture<MongoDbFixture> { }
