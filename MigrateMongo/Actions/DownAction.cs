using System.Reflection;
using MongoDB.Driver;

namespace MigrateMongo.Actions;

/// <summary>
/// Undo the last applied database migration.
/// Mirrors migrate-mongo's <c>down</c> command.
/// </summary>
internal static class DownAction
{
    internal static async Task<IReadOnlyList<string>> ExecuteAsync(
        IMongoDatabase db,
        IMongoClient client,
        Assembly migrationsAssembly,
        bool block = false,
        string? configFilePath = null,
        CancellationToken cancellationToken = default)
    {
        var config = await ConfigManager.ReadAsync(configFilePath, cancellationToken);
        var changelogCollection = db.GetCollection<ChangelogEntry>(config.ChangelogCollectionName);

        // Acquire lock
        await LockManager.AcquireAsync(db, config, cancellationToken);

        try
        {
            var allMigrations = MigrationsLocator.FindMigrations(migrationsAssembly);
            var appliedEntries = await GetAppliedEntriesAsync(changelogCollection, cancellationToken);

            if (appliedEntries.Count == 0)
            {
                return [];
            }

            // Sort applied entries by appliedAt descending to find the last one(s)
            var sortedApplied = appliedEntries.OrderByDescending(e => e.AppliedAt).ToList();

            var entriesToRevert = block
                ? GetBlockEntries(sortedApplied)
                : [sortedApplied[0]];

            var migrationsByFileName = allMigrations.ToDictionary(m => m.FileName);
            var migratedDown = new List<string>();

            foreach (var entry in entriesToRevert)
            {
                if (!migrationsByFileName.TryGetValue(entry.FileName, out var migration))
                {
                    throw new InvalidOperationException(
                        $"Could not find migration class for '{entry.FileName}'. " +
                        "Make sure the migration class is included in the provided assembly.");
                }

                await migration.Instance.DownAsync(db, client, cancellationToken);

                var deleteFilter = Builders<ChangelogEntry>.Filter.Eq(e => e.Id, entry.Id);
                await changelogCollection.DeleteOneAsync(deleteFilter, cancellationToken);

                migratedDown.Add(entry.FileName);
            }

            return migratedDown;
        }
        finally
        {
            await LockManager.ReleaseAsync(db, config, cancellationToken);
        }
    }

    /// <summary>
    /// Get all entries that share the same appliedAt timestamp as the last entry (block revert).
    /// </summary>
    private static List<ChangelogEntry> GetBlockEntries(List<ChangelogEntry> sortedEntries)
    {
        if (sortedEntries.Count == 0)
        {
            return [];
        }

        var lastTimestamp = sortedEntries[0].AppliedAt;

        // Get all entries applied at the same time (same migration batch)
        return sortedEntries
            .TakeWhile(e => e.AppliedAt == lastTimestamp)
            .ToList();
    }

    private static async Task<List<ChangelogEntry>> GetAppliedEntriesAsync(
        IMongoCollection<ChangelogEntry> collection,
        CancellationToken cancellationToken)
    {
        return await collection.Find(Builders<ChangelogEntry>.Filter.Empty)
            .ToListAsync(cancellationToken);
    }
}
