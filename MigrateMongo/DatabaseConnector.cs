using MongoDB.Driver;

namespace MigrateMongo;

/// <summary>
/// Connects to MongoDB using the configuration settings.
/// Mirrors migrate-mongo's <c>database.connect()</c>.
/// </summary>
public static class DatabaseConnector
{
    /// <summary>
    /// Connect to MongoDB using the current configuration.
    /// </summary>
    /// <returns>A tuple of the database and client, matching migrate-mongo's <c>{ db, client }</c>.</returns>
    public static async Task<(IMongoDatabase Db, IMongoClient Client)> ConnectAsync(
        string? configFilePath = null,
        CancellationToken cancellationToken = default)
    {
        var config = await ConfigManager.ReadAsync(configFilePath, cancellationToken);
        return Connect(config);
    }

    /// <summary>
    /// Connect to MongoDB using an explicit configuration.
    /// </summary>
    public static (IMongoDatabase Db, IMongoClient Client) Connect(MigrateMongoConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var url = new MongoUrl(config.MongoDB.Url);
        var client = new MongoClient(url);

        // Database name from config takes precedence, fall back to the one in the URL
        var databaseName = config.MongoDB.DatabaseName ?? url.DatabaseName;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException(
                "No database name specified. Set it in the config's DatabaseName property or encode it in the URL.");
        }

        var db = client.GetDatabase(databaseName);
        return (db, client);
    }
}
