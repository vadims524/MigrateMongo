using System.Reflection;
using MigrateMongo.Actions;
using MongoDB.Driver;

namespace MigrateMongo;

/// <summary>
/// Main API entry point for MigrateMongo.
/// Provides the same API surface as migrate-mongo's module exports:
/// <c>init</c>, <c>create</c>, <c>database.connect</c>, <c>config.read</c>,
/// <c>config.set</c>, <c>up</c>, <c>down</c>, <c>status</c>.
/// </summary>
public static class MigrateMongo
{
    /// <summary>
    /// Initialize a new migration project.
    /// Creates a sample config file and a migrations directory.
    /// </summary>
    public static Task InitAsync(string? directory = null, CancellationToken cancellationToken = default)
        => InitAction.ExecuteAsync(directory, cancellationToken);

    /// <summary>
    /// Create a new database migration file with the provided description.
    /// Returns the generated file name.
    /// </summary>
    public static Task<string> CreateAsync(
        string description,
        string? migrationsDir = null,
        string? configFilePath = null,
        CancellationToken cancellationToken = default)
        => CreateAction.ExecuteAsync(description, migrationsDir, configFilePath, cancellationToken);

    /// <summary>
    /// Run all pending database migrations.
    /// Returns the list of file names that were migrated up.
    /// </summary>
    public static Task<IReadOnlyList<string>> UpAsync(
        IMongoDatabase db,
        IMongoClient client,
        Assembly migrationsAssembly,
        string? configFilePath = null,
        CancellationToken cancellationToken = default)
        => UpAction.ExecuteAsync(db, client, migrationsAssembly, configFilePath, cancellationToken);

    /// <summary>
    /// Undo the last applied database migration.
    /// Returns the list of file names that were migrated down.
    /// </summary>
    /// <param name="db">The MongoDB database.</param>
    /// <param name="client">The MongoDB client.</param>
    /// <param name="migrationsAssembly">The assembly containing migration classes.</param>
    /// <param name="block">If true, revert all migrations from the last batch (same timestamp).</param>
    /// <param name="configFilePath">Optional path to the config file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task<IReadOnlyList<string>> DownAsync(
        IMongoDatabase db,
        IMongoClient client,
        Assembly migrationsAssembly,
        bool block = false,
        string? configFilePath = null,
        CancellationToken cancellationToken = default)
        => DownAction.ExecuteAsync(db, client, migrationsAssembly, block, configFilePath, cancellationToken);

    /// <summary>
    /// Get the status of all migrations (applied or pending).
    /// Returns a list of migration statuses.
    /// </summary>
    public static Task<IReadOnlyList<MigrationStatus>> StatusAsync(
        IMongoDatabase db,
        Assembly migrationsAssembly,
        string? configFilePath = null,
        CancellationToken cancellationToken = default)
        => StatusAction.ExecuteAsync(db, migrationsAssembly, configFilePath, cancellationToken);
}
