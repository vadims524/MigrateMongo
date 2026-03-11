namespace MigrateMongo;

/// <summary>
/// Represents the status of a single migration.
/// </summary>
public sealed record MigrationStatus
{
    /// <summary>
    /// The migration file name (e.g. "20160608155948-blacklist_the_beatles.cs").
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// When the migration was applied, or "PENDING" if not yet applied.
    /// </summary>
    public required string AppliedAt { get; init; }

    /// <summary>
    /// The file hash, if file-hash tracking is enabled.
    /// </summary>
    public string? FileHash { get; init; }
}
