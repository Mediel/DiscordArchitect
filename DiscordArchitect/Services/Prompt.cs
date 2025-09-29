namespace DiscordArchitect.Services;

/// <summary>
/// Represents a prompt for user input related to category names.
/// </summary>
public sealed class Prompt
{
    /// <summary>
    /// Prompts the user to enter a new category name and returns the entered value asynchronously.
    /// </summary>
    /// <remarks>If the user enters only whitespace or leaves the input blank, the default value "NewCategory"
    /// is returned.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entered category name, or
    /// "NewCategory" if no name was provided.</returns>
    public Task<string> GetNewCategoryNameAsync()
    {
        Console.Write("Enter new category name: ");
        var name = Console.ReadLine();
        return Task.FromResult(string.IsNullOrWhiteSpace(name) ? "NewCategory" : name);
    }
}
