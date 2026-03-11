namespace MigrateMongo.Actions;

/// <summary>
/// Create a new database migration file with the provided description.
/// Mirrors migrate-mongo's <c>create</c> command.
/// </summary>
internal static class CreateAction
{
    private const string MigrationTemplate = """
        using MongoDB.Driver;
        using MigrateMongo;

        namespace Migrations;

        public sealed class __CLASSNAME__ : IMigration
        {
            public async Task UpAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default)
            {
                // TODO: Write your migration here.
                // Example:
                // var collection = db.GetCollection<MongoDB.Bson.BsonDocument>("albums");
                // var filter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("artist", "The Beatles");
                // var update = Builders<MongoDB.Bson.BsonDocument>.Update.Set("blacklisted", true);
                // await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
                await Task.CompletedTask;
            }

            public async Task DownAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default)
            {
                // TODO: Write the statements to rollback your migration (if possible).
                // Example:
                // var collection = db.GetCollection<MongoDB.Bson.BsonDocument>("albums");
                // var filter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("artist", "The Beatles");
                // var update = Builders<MongoDB.Bson.BsonDocument>.Update.Set("blacklisted", false);
                // await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
                await Task.CompletedTask;
            }
        }
        """;

    internal static async Task<string> ExecuteAsync(
        string description,
        string? migrationsDir = null,
        string? configFilePath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("A description is required to create a migration.", nameof(description));
        }

        var config = await ConfigManager.ReadAsync(configFilePath, cancellationToken);
        var dir = migrationsDir ?? ResolveMigrationsDir(config);
        Directory.CreateDirectory(dir);

        // Check for a custom sample migration file
        var sampleFile = Path.Combine(dir, $"sample-migration{config.MigrationFileExtension}");
        string? customTemplate = null;
        if (File.Exists(sampleFile))
        {
            customTemplate = await File.ReadAllTextAsync(sampleFile, cancellationToken);
        }

        var fileName = MigrationsLocator.GenerateFileName(description, config.MigrationFileExtension);
        var className = MigrationsLocator.FileNameToTypeName(Path.GetFileNameWithoutExtension(fileName));
        var filePath = Path.Combine(dir, fileName);

        var content = customTemplate ?? MigrationTemplate.Replace("__CLASSNAME__", className);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);

        return fileName;
    }

    private static string ResolveMigrationsDir(MigrateMongoConfig config)
    {
        return Path.IsPathRooted(config.MigrationsDir)
            ? config.MigrationsDir
            : Path.Combine(Directory.GetCurrentDirectory(), config.MigrationsDir);
    }
}
