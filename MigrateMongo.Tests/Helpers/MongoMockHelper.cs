using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;

namespace MigrateMongo.Tests.Helpers;

/// <summary>
/// Builds NSubstitute mocks for IMongoDatabase / IMongoCollection wired with
/// a fixed set of changelog entries. Used by UpAction, DownAction and StatusAction unit tests.
/// </summary>
internal static class MongoMockHelper
{
    internal record MongoMocks(
        IMongoDatabase Db,
        IMongoClient Client,
        IMongoCollection<ChangelogEntry> Changelog);

    /// <summary>
    /// Creates a fully wired mock graph pre-loaded with <paramref name="entries"/>.
    /// The <see cref="MongoMocks.Changelog"/> substitute can be used for
    /// NSubstitute <c>Received()</c> assertions after the action under test runs.
    /// </summary>
    internal static MongoMocks Build(IEnumerable<ChangelogEntry>? entries = null)
    {
        var entryList = (entries ?? []).ToList();

        // IAsyncCursor: yield all entries on first MoveNext, then signal end-of-stream.
        var cursor = Substitute.For<IAsyncCursor<ChangelogEntry>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(entryList.Count > 0), Task.FromResult(false));
        cursor.Current.Returns(entryList);

        // IMongoCollection<ChangelogEntry>: intercept FindAsync (called by Find().ToListAsync()).
        // InsertOneAsync / DeleteOneAsync use NSubstitute defaults: Task.CompletedTask / Task.FromResult(null).
        var changelog = Substitute.For<IMongoCollection<ChangelogEntry>>();
        changelog.FindAsync(
                Arg.Any<FilterDefinition<ChangelogEntry>>(),
                Arg.Any<FindOptions<ChangelogEntry, ChangelogEntry>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IAsyncCursor<ChangelogEntry>>(cursor));

        // IMongoCollection<BsonDocument>: used by LockManager (acquire / release).
        // LockTtl = 0 in test config skips the Indexes.CreateOneAsync path entirely.
        var lockColl = Substitute.For<IMongoCollection<BsonDocument>>();

        // IMongoDatabase: route generic GetCollection<T> calls to the appropriate substitute.
        var db = Substitute.For<IMongoDatabase>();
        db.GetCollection<ChangelogEntry>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
          .Returns(changelog);
        db.GetCollection<BsonDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
          .Returns(lockColl);

        var client = Substitute.For<IMongoClient>();

        return new MongoMocks(db, client, changelog);
    }
}
