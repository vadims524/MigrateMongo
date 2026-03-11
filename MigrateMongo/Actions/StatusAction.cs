using System.Reflection;
using MongoDB.Driver;

namespace MigrateMongo.Actions;

/// <summary>
/// Print the changelog of the database, showing which migrations are applied or pending.
/// Mirrors migrate-mongo's <c>status</c> command.
/// </summary>
internal static class StatusAction
{
    internal static async Task<IReadOnlyList<MigrationStatus>> ExecuteAsync(
        IMongoDatabase db,
        Assembly migrationsAssembly,
        string? configFilePath = null,
        CancellationToken cancellationToken = default)
    {
        var config = await ConfigManager.ReadAsync(configFilePath, cancellationToken);
        var changelogCollection = db.GetCollection<ChangelogEntry>(config.ChangelogCollectionName);

        var allMigrations = MigrationsLocator.FindMigrations(migrationsAssembly);
        var appliedEntries = await changelogCollection
            .Find(Builders<ChangelogEntry>.Filter.Empty)
            .ToListAsync(cancellationToken);

        var appliedByFileName = appliedEntries.ToDictionary(e => e.FileName);

        var statusList = new List<MigrationStatus>();

        foreach (var migration in allMigrations)
        {
            if (appliedByFileName.TryGetValue(migration.FileName, out var entry))
            {
                var status = new MigrationStatus
                {
                    FileName = migration.FileName,
                    AppliedAt = entry.AppliedAt.ToString("o"),
                    FileHash = entry.FileHash
                };

                // If using file hash, check if the file has changed
                if (config.UseFileHash && entry.FileHash is not null)
                {
                    var currentHash = ComputeMigrationHash(migration, config);
                    if (currentHash is not null && currentHash != entry.FileHash)
                    {
                        status = status with { AppliedAt = "PENDING (file changed)" };
                    }
                }

                statusList.Add(status);
            }
            else
            {
                statusList.Add(new MigrationStatus
                {
                    FileName = migration.FileName,
                    AppliedAt = "PENDING"
                });
            }
        }

        return statusList;
    }

    private static string? ComputeMigrationHash(MigrationsLocator.MigrationInfo migration, MigrateMongoConfig config)
    {
        var dir = Path.IsPathRooted(config.MigrationsDir)
            ? config.MigrationsDir
            : Path.Combine(Directory.GetCurrentDirectory(), config.MigrationsDir);

        var filePath = Path.Combine(dir, migration.FileName + config.MigrationFileExtension);
        return File.Exists(filePath) ? MigrationsLocator.ComputeFileHash(filePath) : null;
    }
}
