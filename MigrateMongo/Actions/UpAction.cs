using System.Reflection;
using MongoDB.Driver;

namespace MigrateMongo.Actions;

/// <summary>
/// Run all pending database migrations.
/// Mirrors migrate-mongo's <c>up</c> command.
/// </summary>
internal static class UpAction
{
    internal static async Task<IReadOnlyList<string>> ExecuteAsync(
        IMongoDatabase db,
        IMongoClient client,
        Assembly migrationsAssembly,
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
            var appliedFileNames = new HashSet<string>(appliedEntries.Select(e => e.FileName));

            // When useFileHash is enabled, also consider hash changes
            var appliedHashes = config.UseFileHash
                ? appliedEntries
                    .Where(e => e.FileHash is not null)
                    .ToDictionary(e => e.FileName, e => e.FileHash!)
                : null;

            var migrated = new List<string>();

            foreach (var migration in allMigrations)
            {
                var isPending = !appliedFileNames.Contains(migration.FileName);

                // If using file hash, also re-run if hash changed
                if (!isPending && config.UseFileHash && appliedHashes is not null)
                {
                    var currentHash = ComputeMigrationHash(migration, config);
                    if (appliedHashes.TryGetValue(migration.FileName, out var storedHash) && storedHash != currentHash)
                    {
                        // Hash changed: remove old entry so it can be re-applied
                        var deleteFilter = Builders<ChangelogEntry>.Filter.Eq(e => e.FileName, migration.FileName);
                        await changelogCollection.DeleteOneAsync(deleteFilter, cancellationToken);
                        isPending = true;
                    }
                }

                if (!isPending)
                {
                    continue;
                }

                await migration.Instance.UpAsync(db, client, cancellationToken);

                var entry = new ChangelogEntry
                {
                    FileName = migration.FileName,
                    AppliedAt = DateTime.UtcNow
                };

                if (config.UseFileHash)
                {
                    entry.FileHash = ComputeMigrationHash(migration, config);
                }

                await changelogCollection.InsertOneAsync(entry, cancellationToken: cancellationToken);
                migrated.Add(migration.FileName);
            }

            return migrated;
        }
        finally
        {
            await LockManager.ReleaseAsync(db, config, cancellationToken);
        }
    }

    private static async Task<List<ChangelogEntry>> GetAppliedEntriesAsync(
        IMongoCollection<ChangelogEntry> collection,
        CancellationToken cancellationToken)
    {
        return await collection.Find(Builders<ChangelogEntry>.Filter.Empty)
            .ToListAsync(cancellationToken);
    }

    private static string? ComputeMigrationHash(MigrationsLocator.MigrationInfo migration, MigrateMongoConfig config)
    {
        // Try to find the source file in the migrations directory
        var dir = Path.IsPathRooted(config.MigrationsDir)
            ? config.MigrationsDir
            : Path.Combine(Directory.GetCurrentDirectory(), config.MigrationsDir);

        var filePath = Path.Combine(dir, migration.FileName + config.MigrationFileExtension);
        return File.Exists(filePath) ? MigrationsLocator.ComputeFileHash(filePath) : null;
    }
}
