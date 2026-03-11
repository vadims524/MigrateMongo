# migrate-mongo CLI

A standalone command-line tool for running [MigrateMongo](../MigrateMongo/README.md) database migrations against MongoDB.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A running MongoDB instance

## Installation

### As a global .NET tool

```shell
dotnet tool install -g MigrateMongo.Cli
migrate-mongo --help
```

### From source (development)

```shell
dotnet run --project MigrateMongo.Cli -- --help
```

All examples below use `migrate-mongo`. Replace with `dotnet run --project MigrateMongo.Cli --` when running from source.

---

## Quick start

```shell
# 1. Scaffold config and migrations folder
migrate-mongo init

# 2. Edit connection string
#    mongo-migrate-config.json → "connectionString": "mongodb://localhost:27017"

# 3. Create your first migration
migrate-mongo create add_user_index

# 4. Implement Up/Down in the generated file, then apply
migrate-mongo up

# 5. Check what has been applied
migrate-mongo status
```

---

## Commands

### `init`

Creates `mongo-migrate-config.json` and the `migrations/` directory in the target directory.

```shell
migrate-mongo init [--directory <dir>]
```

| Option | Description |
|---|---|
| `--directory` | Directory to initialise (default: current directory) |

**Example**

```shell
migrate-mongo init --directory ./my-app
```

```
Initialised successfully.
  → Edit mongo-migrate-config.json with your MongoDB connection string.
  → Add IMigration classes to the migrations/ directory.
```

---

### `create`

Scaffolds a new timestamped migration file.

```shell
migrate-mongo create <description> [--migrations-dir <dir>] [--config <file>]
```

| Argument / Option | Description |
|---|---|
| `description` | Short label used in the file name (e.g. `add_user_index`) |
| `--migrations-dir` | Override the migrations directory from config |
| `--config`, `-c` | Path to config file |

**Example**

```shell
migrate-mongo create add_user_index
```

```
Created: migrations/20250115123045_add_user_index.cs
```

The generated file contains a class that implements `IMigration` with empty `UpAsync` and `DownAsync` stubs.

To use a custom template, place a `sample-migration.cs` file in your migrations directory. Use `__CLASSNAME__` as the placeholder for the generated class name.

---

### `up`

Applies all pending migrations in timestamp order.

```shell
migrate-mongo up [--assembly <dll>] [--config <file>]
```

| Option | Description |
|---|---|
| `--assembly`, `-a` | Path to the compiled `.dll` containing your `IMigration` classes |
| `--config`, `-c` | Path to config file |

**Example**

```shell
migrate-mongo up --assembly ./MyApp/bin/Release/net10.0/MyApp.dll
```

```
MIGRATED UP: 20250115123045_add_user_index
MIGRATED UP: 20250115130000_seed_admin_user
```

If there is nothing to apply:

```
No pending migrations.
```

---

### `down`

Reverts the last applied migration. Pass `--block` to revert all migrations from the same batch (those sharing the same `AppliedAt` timestamp).

```shell
migrate-mongo down [--block] [--assembly <dll>] [--config <file>]
```

| Option | Description |
|---|---|
| `--block` | Revert the entire last batch instead of just one migration |
| `--assembly`, `-a` | Path to the compiled `.dll` containing your `IMigration` classes |
| `--config`, `-c` | Path to config file |

**Example**

```shell
migrate-mongo down
```

```
MIGRATED DOWN: 20250115130000_seed_admin_user
```

```shell
migrate-mongo down --block
```

```
MIGRATED DOWN: 20250115130000_seed_admin_user
MIGRATED DOWN: 20250115123045_add_user_index
```

If there is nothing to revert:

```
No migrations to revert.
```

---

### `status`

Displays every discovered migration and whether it has been applied.

```shell
migrate-mongo status [--assembly <dll>] [--config <file>]
```

| Option | Description |
|---|---|
| `--assembly`, `-a` | Path to the compiled `.dll` containing your `IMigration` classes |
| `--config`, `-c` | Path to config file |

**Example output**

```
┌────────────────────────────────────────┬──────────────────────────┐
│ File Name                              │ Applied At               │
├────────────────────────────────────────┼──────────────────────────┤
│ 20250115123045_add_user_index          │ 2025-01-15T12:31:02.000Z │
│ 20250115130000_seed_admin_user         │ PENDING                  │
└────────────────────────────────────────┴──────────────────────────┘
```

Applied migrations are shown in **green**. Pending migrations are shown in **yellow**.

---

## Shared options

These options are accepted by `create`, `up`, `down`, and `status`.

| Option | Short | Description | Default |
|---|---|---|---|
| `--config` | `-c` | Path to `mongo-migrate-config.json` | `mongo-migrate-config.json` in the current directory |
| `--assembly` | `-a` | Path to the `.dll` containing `IMigration` classes | The CLI entry assembly |

---

## Config file

`mongo-migrate-config.json` is created by `migrate-mongo init` and contains:

```json
{
  "connectionString": "mongodb://localhost:27017",
  "database": "my_database",
  "migrationsDir": "migrations",
  "changelogCollectionName": "changelog",
  "lockCollectionName": "changelog_lock",
  "lockTtl": 0,
  "useFileHash": false
}
```

See the [library README](../MigrateMongo/README.md) for full configuration reference.

---

## Migration assembly modes

### Standalone — migrations in a separate project

Compile your migrations project and pass the output `.dll`:

```shell
migrate-mongo up --config ./config/mongo-migrate-config.json \
                 --assembly ./MyApp.Migrations/bin/Release/net10.0/MyApp.Migrations.dll
```

### Embedded — migrations inside this CLI project

When `--assembly` is omitted the CLI falls back to `Assembly.GetEntryAssembly()`. This lets you place `IMigration` classes directly in a custom CLI project that references `MigrateMongo`:

```shell
my-custom-cli up --config mongo-migrate-config.json
```

---

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Command completed successfully |
| `1` | An error occurred; the message is printed to **stderr** |

Errors are written to `stderr` in red and prefixed with `ERROR:`:

```
ERROR: Connection refused (mongodb://localhost:27017).
```

---

## Per-command help

Every command supports `--help`:

```shell
migrate-mongo --help
migrate-mongo up --help
migrate-mongo down --help
```
