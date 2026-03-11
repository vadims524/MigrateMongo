using MongoDB.Driver;

namespace MigrateMongo;

/// <summary>
/// Represents a database migration with up and down operations.
/// Implement this interface to create migration classes.
/// </summary>
public interface IMigration
{
    /// <summary>
    /// Apply the migration.
    /// </summary>
    Task UpAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revert the migration.
    /// </summary>
    Task DownAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default);
}
