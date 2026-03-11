using MigrateMongo;

namespace MigrateMongo.Cli;

/// <summary>
/// Shared console output helpers: coloured messages and the status table.
/// </summary>
internal static class Output
{
    internal static void Success(string message) => WriteLine(ConsoleColor.Green, message);
    internal static void Info(string message)    => WriteLine(ConsoleColor.Cyan,  message);

    internal static void Fail(string message)
        => WriteLine(ConsoleColor.Red, $"ERROR: {message}", Console.Error);

    /// <summary>
    /// Renders a Unicode box table of migration statuses, mirroring migrate-mongo's output.
    /// Pending migrations are shown in yellow, applied ones in green.
    /// </summary>
    internal static void Table(IReadOnlyList<MigrationStatus> statuses)
    {
        if (statuses.Count == 0)
        {
            Info("No migrations found.");
            return;
        }

        int fnWidth = Math.Max("File Name".Length,  statuses.Max(s => s.FileName.Length));
        int atWidth = Math.Max("Applied At".Length, statuses.Max(s => s.AppliedAt.Length));

        Border('┌', '┬', '┐', '─', fnWidth, atWidth);
        Console.WriteLine($"│ {"File Name".PadRight(fnWidth)} │ {"Applied At".PadRight(atWidth)} │");
        Border('├', '┼', '┤', '─', fnWidth, atWidth);

        foreach (var s in statuses)
        {
            var pending = s.AppliedAt == "PENDING";
            Console.Write($"│ {s.FileName.PadRight(fnWidth)} │ ");
            Console.ForegroundColor = pending ? ConsoleColor.Yellow : ConsoleColor.Green;
            Console.Write(s.AppliedAt.PadRight(atWidth));
            Console.ResetColor();
            Console.WriteLine(" │");
        }

        Border('└', '┴', '┘', '─', fnWidth, atWidth);
    }

    private static void WriteLine(ConsoleColor color, string message, TextWriter? writer = null)
    {
        Console.ForegroundColor = color;
        (writer ?? Console.Out).WriteLine(message);
        Console.ResetColor();
    }

    private static void Border(char left, char mid, char right, char fill, int col1, int col2)
        => Console.WriteLine($"{left}{Pad(fill, col1 + 2)}{mid}{Pad(fill, col2 + 2)}{right}");

    private static string Pad(char c, int count) => new(c, count);
}
