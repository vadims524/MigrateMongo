using System.Text.Json.Serialization;

namespace MigrateMongo;

/// <summary>
/// Configuration for MigrateMongo, mirroring migrate-mongo-config.js.
/// </summary>
public sealed record MigrateMongoConfig
{
    /// <summary>
    /// MongoDB connection settings.
    /// </summary>
    public required MongoDbSettings MongoDB { get; init; }

    /// <summary>
    /// The migrations directory (relative or absolute path).
    /// </summary>
    public string MigrationsDir { get; init; } = "migrations";

    /// <summary>
    /// The MongoDB collection where applied changes are stored.
    /// </summary>
    public string ChangelogCollectionName { get; init; } = "changelog";

    /// <summary>
    /// The MongoDB collection where the lock will be created.
    /// </summary>
    public string LockCollectionName { get; init; } = "changelog_lock";

    /// <summary>
    /// The value in seconds for the TTL index used for the lock. 0 disables the feature.
    /// </summary>
    public int LockTtl { get; init; }

    /// <summary>
    /// The file extension for migration source files.
    /// </summary>
    public string MigrationFileExtension { get; init; } = ".cs";

    /// <summary>
    /// Enable file-hash-based change detection so updated migrations can be re-run.
    /// </summary>
    public bool UseFileHash { get; init; }
}

/// <summary>
/// MongoDB connection settings.
/// </summary>
public sealed record MongoDbSettings
{
    /// <summary>
    /// The MongoDB connection URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The database name. Can also be encoded in the URL.
    /// </summary>
    public string? DatabaseName { get; init; }

    /// <summary>
    /// Additional MongoDB client options (key-value pairs forwarded to MongoClientSettings).
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Options { get; init; }
}
