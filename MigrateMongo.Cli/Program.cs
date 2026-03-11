using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using MigrateMongo;
using MigrateMongo.Cli;

// The MigrateMongo class shares its name with the library namespace, so use an alias.
using Api = global::MigrateMongo.MigrateMongo;

// ── Shared options ────────────────────────────────────────────────────────────

var configOpt = new Option<FileInfo?>(
    aliases: ["--config", "-c"],
    description: "Path to the config file (default: mongo-migrate-config.json in the current directory)");

var assemblyOpt = new Option<FileInfo?>(
    aliases: ["--assembly", "-a"],
    description: "Path to the compiled .dll that contains your IMigration classes "
               + "(default: the entry assembly — useful when migrations live in this project)");

// ── init ──────────────────────────────────────────────────────────────────────

var initDirOpt = new Option<DirectoryInfo?>(
    "--directory",
    "Directory to initialise (default: current directory)");

var initCmd = new Command("init", "Create the config file and migrations directory");
initCmd.AddOption(initDirOpt);
initCmd.SetHandler(async ctx =>
{
    var dir = ctx.ParseResult.GetValueForOption(initDirOpt);
    try
    {
        await Api.InitAsync(dir?.FullName);
        Output.Success("Initialised successfully.");
        Output.Info("  → Edit mongo-migrate-config.json with your MongoDB connection string.");
        Output.Info("  → Add IMigration classes to the migrations/ directory.");
    }
    catch (Exception ex)
    {
        Output.Fail(ex.Message);
        ctx.ExitCode = 1;
    }
});

// ── create ────────────────────────────────────────────────────────────────────

var descArg = new Argument<string>(
    "description",
    "Short description used in the migration file name (e.g. \"add_user_index\")");

var migDirOpt = new Option<DirectoryInfo?>(
    "--migrations-dir",
    "Override the migrations directory from config");

var createCmd = new Command("create", "Scaffold a new timestamped migration file");
createCmd.AddArgument(descArg);
createCmd.AddOption(migDirOpt);
createCmd.AddOption(configOpt);
createCmd.SetHandler(async ctx =>
{
    var desc   = ctx.ParseResult.GetValueForArgument(descArg);
    var migDir = ctx.ParseResult.GetValueForOption(migDirOpt);
    var config = ctx.ParseResult.GetValueForOption(configOpt);
    try
    {
        var file = await Api.CreateAsync(desc, migDir?.FullName, config?.FullName);
        Output.Success($"Created: {file}");
    }
    catch (Exception ex)
    {
        Output.Fail(ex.Message);
        ctx.ExitCode = 1;
    }
});

// ── up ────────────────────────────────────────────────────────────────────────

var upCmd = new Command("up", "Apply all pending migrations in timestamp order");
upCmd.AddOption(assemblyOpt);
upCmd.AddOption(configOpt);
upCmd.SetHandler(async ctx =>
{
    var asmFile = ctx.ParseResult.GetValueForOption(assemblyOpt);
    var config  = ctx.ParseResult.GetValueForOption(configOpt);
    try
    {
        var (db, client) = await DatabaseConnector.ConnectAsync(config?.FullName);
        var asm          = LoadAssembly(asmFile);
        var applied      = await Api.UpAsync(db, client, asm, config?.FullName);

        if (applied.Count == 0)
            Output.Info("No pending migrations.");
        else
            foreach (var name in applied)
                Output.Success($"MIGRATED UP: {name}");
    }
    catch (Exception ex)
    {
        Output.Fail(ex.Message);
        ctx.ExitCode = 1;
    }
});

// ── down ──────────────────────────────────────────────────────────────────────

var blockOpt = new Option<bool>(
    "--block",
    "Revert all migrations from the last batch (those sharing the same AppliedAt timestamp)");

var downCmd = new Command("down", "Revert the last applied migration");
downCmd.AddOption(blockOpt);
downCmd.AddOption(assemblyOpt);
downCmd.AddOption(configOpt);
downCmd.SetHandler(async ctx =>
{
    var block   = ctx.ParseResult.GetValueForOption(blockOpt);
    var asmFile = ctx.ParseResult.GetValueForOption(assemblyOpt);
    var config  = ctx.ParseResult.GetValueForOption(configOpt);
    try
    {
        var (db, client) = await DatabaseConnector.ConnectAsync(config?.FullName);
        var asm          = LoadAssembly(asmFile);
        var reverted     = await Api.DownAsync(db, client, asm, block, config?.FullName);

        if (reverted.Count == 0)
            Output.Info("No migrations to revert.");
        else
            foreach (var name in reverted)
                Output.Success($"MIGRATED DOWN: {name}");
    }
    catch (Exception ex)
    {
        Output.Fail(ex.Message);
        ctx.ExitCode = 1;
    }
});

// ── status ────────────────────────────────────────────────────────────────────

var statusCmd = new Command("status", "Show the status of every discovered migration");
statusCmd.AddOption(assemblyOpt);
statusCmd.AddOption(configOpt);
statusCmd.SetHandler(async ctx =>
{
    var asmFile = ctx.ParseResult.GetValueForOption(assemblyOpt);
    var config  = ctx.ParseResult.GetValueForOption(configOpt);
    try
    {
        var (db, _) = await DatabaseConnector.ConnectAsync(config?.FullName);
        var asm     = LoadAssembly(asmFile);
        var statuses = await Api.StatusAsync(db, asm, config?.FullName);
        Output.Table(statuses);
    }
    catch (Exception ex)
    {
        Output.Fail(ex.Message);
        ctx.ExitCode = 1;
    }
});

// ── root ──────────────────────────────────────────────────────────────────────

var root = new RootCommand("MigrateMongo CLI — database migration tool for MongoDB");
root.AddCommand(initCmd);
root.AddCommand(createCmd);
root.AddCommand(upCmd);
root.AddCommand(downCmd);
root.AddCommand(statusCmd);

return await root.InvokeAsync(args);

// ── Helpers ───────────────────────────────────────────────────────────────────

static Assembly LoadAssembly(FileInfo? file)
{
    if (file is not null)
        return Assembly.LoadFrom(file.FullName);

    return Assembly.GetEntryAssembly()
        ?? throw new InvalidOperationException(
               "Could not determine the entry assembly. Provide the migrations assembly with --assembly <path>.");
}
