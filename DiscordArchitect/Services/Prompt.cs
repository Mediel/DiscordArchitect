using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordArchitect.Services;

/// <summary>
/// Console prompts for values required at runtime (e.g. new category name).
/// </summary>
public sealed class Prompt
{
    /// <summary>Discord channel/category name limit.</summary>
    public const int MaxCategoryNameLength = CategoryNameLineParser.MaxLength;

    private readonly ILogger<Prompt> _log;

    public Prompt(ILogger<Prompt> log)
    {
        _log = log;
    }

    /// <summary>
    /// Prompts for the name of the new category to create (clone target).
    /// </summary>
    /// <remarks>Trims input; re-prompts on empty or over-length names. Non-interactive stdin uses "NewCategory".</remarks>
    public Task<string> GetNewCategoryNameAsync(SocketGuild guild, string templateCategoryName, CancellationToken cancellationToken = default)
    {
        if (Console.IsInputRedirected)
        {
            _log.LogWarning("Standard input is redirected; using default category name \"NewCategory\".");
            return Task.FromResult("NewCategory");
        }

        PrintBanner(guild, templateCategoryName);

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Name: ");
            Console.ResetColor();

            string? line;
            try
            {
                line = Console.ReadLine();
            }
            catch (IOException)
            {
                _log.LogWarning("Input unavailable; using default category name \"NewCategory\".");
                return Task.FromResult("NewCategory");
            }

            if (line == null)
            {
                _log.LogWarning("End of input; using default category name \"NewCategory\".");
                return Task.FromResult("NewCategory");
            }

            var outcome = CategoryNameLineParser.TryParse(line, out var trimmed);
            switch (outcome)
            {
                case CategoryNameLineParser.ParseOutcome.Empty:
                    WriteHint("Name cannot be empty — type a name or press Ctrl+C to abort.");
                    continue;
                case CategoryNameLineParser.ParseOutcome.TooLong:
                    WriteHint($"Discord allows at most {MaxCategoryNameLength} characters (you entered {trimmed.Length}).");
                    continue;
                case CategoryNameLineParser.ParseOutcome.Ok:
                    _log.LogInformation("New category name: {CategoryName}", trimmed);
                    return Task.FromResult(trimmed);
            }
        }

        throw new OperationCanceledException();
    }

    private static void WriteHint(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  {message}");
        Console.ResetColor();
    }

    private static void PrintBanner(SocketGuild guild, string templateCategoryName)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine("  New category");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.ResetColor();
        Console.WriteLine($"  Server:            {guild.Name}");
        Console.WriteLine($"  Template (clone):  {templateCategoryName}");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Max {MaxCategoryNameLength} characters · leading/trailing spaces are trimmed");
        Console.ResetColor();
        Console.WriteLine();
    }
}
