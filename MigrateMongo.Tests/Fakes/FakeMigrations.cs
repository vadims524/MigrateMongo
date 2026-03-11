using MongoDB.Driver;

namespace MigrateMongo.Tests.Fakes;

// Class names intentionally follow Migration_YYYYMMDDHHMMSS_Description so
// MigrationsLocator.FindMigrations can discover and order them by timestamp.

public sealed class Migration_20210101000001_First : IMigration
{
    public Task UpAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DownAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class Migration_20210101000002_Second : IMigration
{
    public Task UpAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DownAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
