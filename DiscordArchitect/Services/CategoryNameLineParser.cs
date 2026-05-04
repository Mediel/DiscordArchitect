namespace DiscordArchitect.Services;

/// <summary>
/// Pure parsing rules for a single console line when entering a Discord category name.
/// </summary>
public static class CategoryNameLineParser
{
    /// <summary>Discord channel/category name length limit.</summary>
    public const int MaxLength = 100;

    /// <summary>Outcome of validating one line of user input (after ReadLine).</summary>
    public enum ParseOutcome
    {
        /// <summary>Trimmed value is empty.</summary>
        Empty,
        /// <summary>Trimmed value exceeds <see cref="MaxLength"/>.</summary>
        TooLong,
        /// <summary>Valid name; trimmed value is ready to use.</summary>
        Ok
    }

    /// <summary>
    /// Evaluates a non-null line from stdin. Trims leading and trailing whitespace.
    /// </summary>
    public static ParseOutcome TryParse(string line, out string trimmed)
    {
        trimmed = line.Trim();
        if (trimmed.Length == 0)
            return ParseOutcome.Empty;
        if (trimmed.Length > MaxLength)
            return ParseOutcome.TooLong;
        return ParseOutcome.Ok;
    }
}
