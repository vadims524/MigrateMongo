using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MigrateMongo;

/// <summary>
/// Discovers and orders migration classes from an assembly or migration files from a directory.
/// Mirrors migrate-mongo's migrationsDir module.
/// </summary>
public static partial class MigrationsLocator
{
    // Matches the timestamp prefix pattern: 20160608155948-description
    [GeneratedRegex(@"^(\d{14})[-_](.+)$")]
    private static partial Regex TimestampPattern();

    /// <summary>
    /// Represents a discovered migration with its metadata.
    /// </summary>
    internal sealed record MigrationInfo(
        string FileName,
        long Timestamp,
        IMigration Instance);

    /// <summary>
    /// Find all migration classes in the given assembly, ordered by their timestamp.
    /// Migration class names must follow the pattern: Migration_YYYYMMDDHHMMSS_Description.
    /// </summary>
    internal static IReadOnlyList<MigrationInfo> FindMigrations(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var migrationTypes = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IMigration).IsAssignableFrom(t))
            .ToList();

        var migrations = new List<MigrationInfo>();

        foreach (var type in migrationTypes)
        {
            var fileName = TypeNameToFileName(type.Name);
            var timestamp = ExtractTimestamp(fileName);

            if (timestamp is null)
            {
                throw new InvalidOperationException(
                    $"Migration class '{type.Name}' does not follow the naming convention. " +
                    "Expected: Migration_YYYYMMDDHHMMSS_Description");
            }

            var instance = (IMigration)(Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Failed to create instance of migration '{type.Name}'."));

            migrations.Add(new MigrationInfo(fileName, timestamp.Value, instance));
        }

        return migrations.OrderBy(m => m.Timestamp).ToList();
    }

    /// <summary>
    /// Generate a timestamp-prefixed file name for a new migration.
    /// Matches migrate-mongo's naming: YYYYMMDDHHMMSS-description.
    /// </summary>
    internal static string GenerateFileName(string description, string extension)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var sanitized = SanitizeDescription(description);
        return $"{timestamp}-{sanitized}{extension}";
    }

    /// <summary>
    /// Generate a valid C# class name from a migration file name.
    /// </summary>
    internal static string FileNameToTypeName(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        // Replace hyphens and other invalid chars with underscores
        var className = Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9_]", "_");
        return $"Migration_{className}";
    }

    /// <summary>
    /// Convert a type name back to the original file name pattern.
    /// </summary>
    internal static string TypeNameToFileName(string typeName)
    {
        // Remove the "Migration_" prefix if present
        var name = typeName.StartsWith("Migration_", StringComparison.Ordinal)
            ? typeName["Migration_".Length..]
            : typeName;

        // Replace underscores back to hyphens for the timestamp separator
        // Pattern: YYYYMMDDHHMMSS_description -> YYYYMMDDHHMMSS-description
        if (name.Length > 14 && name[14] == '_')
        {
            name = string.Concat(name.AsSpan(0, 14), "-", name.AsSpan(15));
        }

        return name;
    }

    /// <summary>
    /// Extract the numeric timestamp from a file name.
    /// </summary>
    internal static long? ExtractTimestamp(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var match = TimestampPattern().Match(nameWithoutExtension);

        if (!match.Success)
        {
            return null;
        }

        return long.TryParse(match.Groups[1].Value, out var timestamp) ? timestamp : null;
    }

    /// <summary>
    /// Compute a SHA-256 hash of the file contents, used when useFileHash is enabled.
    /// </summary>
    internal static string ComputeFileHash(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    private static string SanitizeDescription(string description)
    {
        // Replace spaces and special characters with underscores
        return Regex.Replace(description.Trim(), @"[^a-zA-Z0-9_]", "_").ToLowerInvariant();
    }
}
