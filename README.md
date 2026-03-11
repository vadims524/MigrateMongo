# MigrateMongo

A .NET port of the Node.js [`migrate-mongo`](https://github.com/seppevs/migrate-mongo) library. Provides the same workflow — `init`, `create`, `up`, `down`, `status` — as a strongly-typed C# API backed by MongoDB.Driver 3.x.

## Requirements

- .NET 10
- MongoDB 5.0+

## Installation

Add a project reference or reference the `MigrateMongo` project directly:

```xml
<ProjectReference Include="..\MigrateMongo\MigrateMongo.csproj" />
```

---

## Quick Start

```csharp
using MigrateMongo;
using MongoDB.Driver;

// 1. Configure once at startup (alternative: use a config file — see Configuration below)
ConfigManager.Set(new MigrateMongoConfig
{
    MongoDB = new MongoDbSettings
    {
        Url = "mongodb://localhost:27017",
        DatabaseName = "myapp"
    }
});

// 2. Connect
var (db, client) = await DatabaseConnector.ConnectAsync();

// 3. Run all pending migrations
var migrationsAssembly = typeof(Program).Assembly; // assembly that contains your IMigration classes
var applied = await MigrateMongo.UpAsync(db, client, migrationsAssembly);

Console.WriteLine($"Applied {applied.Count} migration(s).");
```

---

## Configuration

### Programmatic (recommended for applications)

Call `ConfigManager.Set()` before any other API call:

```csharp
ConfigManager.Set(new MigrateMongoConfig
{
    MongoDB = new MongoDbSettings
    {
        Url = "mongodb://localhost:27017",
        DatabaseName = "myapp"
    },
    MigrationsDir          = "migrations",      // default
    ChangelogCollectionName = "changelog",       // default
    LockCollectionName      = "changelog_lock",  // default
    LockTtl                 = 0,                 // 0 = disabled; seconds until lock auto-expires
    UseFileHash             = false              // set true to re-run modified migrations
});
```

### Config file

Generate a starter config file with `InitAsync`:

```csharp
await MigrateMongo.InitAsync(); // writes mongo-migrate-config.json + creates migrations/
```

The generated `mongo-migrate-config.json`:

```json
{
  "mongodb": {
    "url": "mongodb://localhost:27017",
    "databaseName": "YOURDATABASENAME"
  },
  "migrationsDir": "migrations",
  "changelogCollectionName": "changelog",
  "lockCollectionName": "changelog_lock",
  "lockTtl": 0,
  "migrationFileExtension": ".cs",
  "useFileHash": false
}
```

When no programmatic config is set, `ConfigManager.ReadAsync()` reads this file from the current working directory (or the path passed to each API method).

### `MigrateMongoConfig` reference

| Property | Default | Description |
|---|---|---|
| `MongoDB.Url` | *(required)* | MongoDB connection string |
| `MongoDB.DatabaseName` | `null` | Overrides the database name in the URL |
| `MigrationsDir` | `"migrations"` | Directory containing migration `.cs` files |
| `ChangelogCollectionName` | `"changelog"` | Collection that tracks applied migrations |
| `LockCollectionName` | `"changelog_lock"` | Collection used for the distributed lock |
| `LockTtl` | `0` | Lock TTL in seconds; `0` disables auto-expiry |
| `MigrationFileExtension` | `".cs"` | File extension for generated migration files |
| `UseFileHash` | `false` | Re-run a migration when its source file changes |

---

## Writing Migrations

### Naming convention

Migration class names must follow the pattern:

```
Migration_YYYYMMDDHHMMSS_Description
```

The timestamp controls execution order. The description becomes part of the file name.

| Class name | File name |
|---|---|
| `Migration_20240101120000_AddUserIndex` | `20240101120000-AddUserIndex.cs` |
| `Migration_20240615093000_BlacklistUsers` | `20240615093000-BlacklistUsers.cs` |

### Creating a migration file

```csharp
string fileName = await MigrateMongo.CreateAsync("add_user_index");
// creates migrations/20240101120000-add_user_index.cs
```

This generates a scaffold you fill in:

```csharp
using MongoDB.Driver;
using MigrateMongo;

namespace Migrations;

public sealed class Migration_20240101120000_add_user_index : IMigration
{
    public async Task UpAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default)
    {
        var collection = db.GetCollection<BsonDocument>("users");
        var keys = Builders<BsonDocument>.IndexKeys.Ascending("email");
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(keys, new() { Unique = true }),
            cancellationToken: cancellationToken);
    }

    public async Task DownAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default)
    {
        var collection = db.GetCollection<BsonDocument>("users");
        await collection.Indexes.DropOneAsync("email_1", cancellationToken);
    }
}
```

### Custom template

Place a file named `sample-migration.cs` (or matching your `MigrationFileExtension`) in your migrations directory. `CreateAsync` will use it instead of the built-in scaffold.

### `IMigration` interface

```csharp
public interface IMigration
{
    Task UpAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default);
    Task DownAsync(IMongoDatabase db, IMongoClient client, CancellationToken cancellationToken = default);
}
```

---

## API Reference

All methods are on the static `MigrateMongo` class.

### `InitAsync`

Creates `mongo-migrate-config.json` and the migrations directory.

```csharp
await MigrateMongo.InitAsync(directory: null);
```

### `CreateAsync`

Generates a new timestamped migration file. Returns the generated file name.

```csharp
string fileName = await MigrateMongo.CreateAsync(
    description: "add_user_index",
    migrationsDir: null,      // defaults to config value
    configFilePath: null);
```

### `UpAsync`

Applies all pending migrations in timestamp order. Returns the list of file names that were applied.

```csharp
IReadOnlyList<string> applied = await MigrateMongo.UpAsync(
    db,
    client,
    migrationsAssembly: typeof(Program).Assembly);
```

### `DownAsync`

Reverts the most recently applied migration. Pass `block: true` to revert all migrations that share the same `AppliedAt` timestamp (i.e. those applied in the same `UpAsync` call). Returns the list of file names that were reverted.

```csharp
// Revert the last single migration
IReadOnlyList<string> reverted = await MigrateMongo.DownAsync(db, client, migrationsAssembly);

// Revert the entire last batch
IReadOnlyList<string> reverted = await MigrateMongo.DownAsync(db, client, migrationsAssembly, block: true);
```

### `StatusAsync`

Returns the status of every discovered migration.

```csharp
IReadOnlyList<MigrationStatus> status = await MigrateMongo.StatusAsync(db, migrationsAssembly);

foreach (var s in status)
    Console.WriteLine($"{s.FileName}  {s.AppliedAt}");
// 20240101120000-AddUserIndex  2024-01-01T12:05:00.000Z
// 20240615093000-BlacklistUsers  PENDING
```

`MigrationStatus.AppliedAt` is either an ISO 8601 timestamp string or `"PENDING"`.

### `DatabaseConnector`

```csharp
// From config file / programmatic config
var (db, client) = await DatabaseConnector.ConnectAsync();

// From an explicit config object
var (db, client) = DatabaseConnector.Connect(config);
```

---

## Distributed Lock

Before applying or reverting migrations, MigrateMongo inserts a document into `changelog_lock`. If another process holds the lock an `InvalidOperationException` is thrown immediately — there is no retry.

Set `LockTtl` (seconds) so a crashed process does not leave a stale lock indefinitely:

```csharp
ConfigManager.Set(new MigrateMongoConfig
{
    ...
    LockTtl = 60 // lock auto-expires after 60 seconds via a MongoDB TTL index
});
```

---

## File Hash Tracking

When `UseFileHash = true`, MigrateMongo stores a SHA-256 hash of each migration class's source file in the changelog. If the file changes after it has been applied, it is treated as pending again on the next `UpAsync` call. This lets you iterate on a migration during development without manually clearing the changelog.

```csharp
ConfigManager.Set(new MigrateMongoConfig
{
    ...
    UseFileHash = true
});
```

> **Warning:** Do not enable `UseFileHash` in production. Re-running a migration that has already been applied to live data is destructive.

---

## Project Structure

```
MigrateMongo\                  # library
│   IMigration.cs              # implement this in your migration files
│   MigrateMongo.cs            # static API entry point
│   MigrateMongoConfig.cs      # configuration records
│   MigrationStatus.cs         # status result record
│   DatabaseConnector.cs       # MongoDB connection helper
│   ConfigManager.cs           # reads / overrides config
│   MigrationsLocator.cs       # discovers IMigration classes by naming convention
│   LockManager.cs             # distributed lock via changelog_lock collection
│   ChangelogEntry.cs          # BSON document stored in changelog collection
└── Actions\
        InitAction.cs
        CreateAction.cs
        UpAction.cs
        DownAction.cs
        StatusAction.cs

MigrateMongo.Tests\            # test project
├── Unit\                      # fast tests — no MongoDB required
│   ├── Actions\
│   └── *.cs
├── Integration\               # require a running MongoDB (Docker / Testcontainers)
│   ├── MongoDbFixture.cs
│   └── UpDownStatusTests.cs
├── Fakes\                     # shared IMigration stubs
└── Helpers\                   # NSubstitute mock wiring
```

---

## Running the Tests

```sh
# Unit tests only (no Docker required)
dotnet test --filter "Category=Unit"

# Integration tests only (requires Docker for Testcontainers)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

Integration tests use [Testcontainers.MongoDb](https://dotnet.testcontainers.org/) to spin up a `mongo:7.0` container automatically. Docker must be running.

---

## CLI

`MigrateMongo.Cli` is a standalone console application that exposes the full library API as a command-line tool, mirroring the `migrate-mongo` Node.js experience.

### Install as a global .NET tool

```sh
dotnet tool install -g MigrateMongo.Cli
```

Or run directly from the repository:

```sh
dotnet run --project MigrateMongo.Cli -- <command> [options]
```

### Commands

#### `init`

Create the config file and migrations directory in the current directory.

```sh
migrate-mongo init
migrate-mongo init --directory /path/to/project
```

Output:
```
Initialised successfully.
  → Edit mongo-migrate-config.json with your MongoDB connection string.
  → Add IMigration classes to the migrations/ directory.
```

#### `create <description>`

Scaffold a new timestamped migration file.

```sh
migrate-mongo create add_user_index
migrate-mongo create "blacklist the beatles" --migrations-dir src/Migrations
```

Output:
```
Created: 20240101120000-add_user_index.cs
```

#### `up`

Apply all pending migrations in timestamp order.

```sh
migrate-mongo up
migrate-mongo up --assembly ./bin/Debug/net10.0/MyApp.dll
```

Output:
```
MIGRATED UP: 20240101120000-add_user_index
MIGRATED UP: 20240615093000-blacklist_the_beatles
```

#### `down`

Revert the last applied migration. Use `--block` to revert the entire last batch (all migrations sharing the same `AppliedAt` timestamp, i.e. those applied in a single `up` run).

```sh
migrate-mongo down
migrate-mongo down --block
migrate-mongo down --assembly ./bin/Debug/net10.0/MyApp.dll
```

Output:
```
MIGRATED DOWN: 20240615093000-blacklist_the_beatles
```

#### `status`

Show the status of every discovered migration.

```sh
migrate-mongo status
migrate-mongo status --assembly ./bin/Debug/net10.0/MyApp.dll
```

Output:
```
┌──────────────────────────────────────────┬────────────────────────────────────┐
│ File Name                                │ Applied At                         │
├──────────────────────────────────────────┼────────────────────────────────────┤
│ 20240101120000-add_user_index            │ 2024-01-01T12:05:00.0000000Z      │
│ 20240615093000-blacklist_the_beatles     │ PENDING                            │
└──────────────────────────────────────────┴────────────────────────────────────┘
```

### Shared options

| Option | Short | Description |
|---|---|---|
| `--config <path>` | `-c` | Path to the config file (default: `mongo-migrate-config.json` in CWD) |
| `--assembly <path>` | `-a` | Path to the compiled `.dll` containing `IMigration` classes |

The `--assembly` flag tells the CLI which compiled assembly to scan for migration classes. If omitted, the entry assembly is used — this is useful when you embed migrations directly inside the CLI project.

### Migration discovery workflow

Because .NET migrations must be compiled before they can run (unlike Node.js `.js` files), the typical workflow is:

1. `migrate-mongo init` — generate config and `migrations/` directory
2. `migrate-mongo create <description>` — scaffold a `.cs` migration file
3. **Compile** your migrations project (`dotnet build`)
4. `migrate-mongo up --assembly bin/Debug/net10.0/MyMigrations.dll` — apply migrations

Alternatively, add your migration classes directly to the `MigrateMongo.Cli` project itself, rebuild, and run without `--assembly`.
