namespace MigrateMongo.Actions;

/// <summary>
/// Initialize a new migration project.
/// Creates the config file and migrations directory.
/// Mirrors migrate-mongo's <c>init</c> command.
/// </summary>
internal static class InitAction
{
    internal static async Task ExecuteAsync(string? directory = null, CancellationToken cancellationToken = default)
    {
        var dir = directory ?? Directory.GetCurrentDirectory();

        // Create the migrations directory
        var migrationsDir = Path.Combine(dir, "migrations");
        Directory.CreateDirectory(migrationsDir);

        // Create the default config file
        await ConfigManager.WriteDefaultAsync(dir, cancellationToken);
    }
}
