using System.Text.Json;
using System.Text.Json.Serialization;

namespace MigrateMongo;

/// <summary>
/// Manages the MongoMigrate configuration file (mongo-migrate-config.json).
/// Mirrors migrate-mongo's config module: read() and set().
/// </summary>
public static class ConfigManager
{
    private const string DefaultConfigFileName = "mongo-migrate-config.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static MigrateMongoConfig? s_overriddenConfig;

    /// <summary>
    /// Override the config so that the config file is NOT used.
    /// Call this at the very beginning of your program.
    /// Mirrors migrate-mongo's <c>config.set()</c>.
    /// </summary>
    public static void Set(MigrateMongoConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        s_overriddenConfig = config;
    }

    /// <summary>
    /// Read the configuration. If <see cref="Set"/> was called, returns that config.
    /// Otherwise reads from the JSON file on disk.
    /// Mirrors migrate-mongo's <c>config.read()</c>.
    /// </summary>
    public static async Task<MigrateMongoConfig> ReadAsync(string? configFilePath = null, CancellationToken cancellationToken = default)
    {
        if (s_overriddenConfig is not null)
        {
            return s_overriddenConfig;
        }

        var filePath = configFilePath ?? Path.Combine(Directory.GetCurrentDirectory(), DefaultConfigFileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Config file not found: {filePath}. Run init first.", filePath);
        }

        await using var stream = File.OpenRead(filePath);
        var config = await JsonSerializer.DeserializeAsync<MigrateMongoConfig>(stream, s_jsonOptions, cancellationToken);

        return config ?? throw new InvalidOperationException("Failed to deserialize config file.");
    }

    /// <summary>
    /// Write a default configuration file to disk.
    /// </summary>
    internal static async Task WriteDefaultAsync(string directory, CancellationToken cancellationToken = default)
    {
        var config = new MigrateMongoConfig
        {
            MongoDB = new MongoDbSettings
            {
                Url = "mongodb://localhost:27017",
                DatabaseName = "YOURDATABASENAME"
            }
        };

        var filePath = Path.Combine(directory, DefaultConfigFileName);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, config, s_jsonOptions, cancellationToken);
    }

    /// <summary>
    /// Clear any overridden config (useful for testing).
    /// </summary>
    internal static void Reset() => s_overriddenConfig = null;
}
