using MongoDB.Bson;
using MongoDB.Driver;

namespace MigrateMongo;

/// <summary>
/// Manages a distributed lock in MongoDB to prevent concurrent migrations.
/// Mirrors migrate-mongo's lock module.
/// </summary>
internal static class LockManager
{
    private const string LockKey = "migration_lock";

    /// <summary>
    /// Acquire a migration lock. Throws if the lock is already held.
    /// </summary>
    internal static async Task AcquireAsync(
        IMongoDatabase db,
        MigrateMongoConfig config,
        CancellationToken cancellationToken = default)
    {
        var collection = db.GetCollection<BsonDocument>(config.LockCollectionName);

        // Create TTL index if lockTtl > 0
        if (config.LockTtl > 0)
        {
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("createdAt");
            var indexOptions = new CreateIndexOptions
            {
                ExpireAfter = TimeSpan.FromSeconds(config.LockTtl)
            };
            var indexModel = new CreateIndexModel<BsonDocument>(indexKeys, indexOptions);
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
        }

        var lockDoc = new BsonDocument
        {
            { "_id", LockKey },
            { "createdAt", DateTime.UtcNow }
        };

        try
        {
            await collection.InsertOneAsync(lockDoc, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new InvalidOperationException(
                "Could not acquire migration lock. Another migration is already running.");
        }
    }

    /// <summary>
    /// Release the migration lock.
    /// </summary>
    internal static async Task ReleaseAsync(
        IMongoDatabase db,
        MigrateMongoConfig config,
        CancellationToken cancellationToken = default)
    {
        var collection = db.GetCollection<BsonDocument>(config.LockCollectionName);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", LockKey);
        await collection.DeleteOneAsync(filter, cancellationToken);
    }
}
