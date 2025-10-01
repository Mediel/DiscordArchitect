using Discord;
using Discord.WebSocket;
using DiscordArchitect.Options;
using DiscordArchitect.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordArchitect.Hosting;

/// <summary>
/// Provides a hosted service that manages the lifecycle of a Discord bot, including connecting to Discord, handling
/// readiness, and performing initial server setup tasks.
/// </summary>
/// <remarks>This service is intended to be registered with the application's dependency injection container and
/// started automatically as part of the application's hosting environment. It handles logging in to Discord, waiting
/// for the client to become ready, and performing initial operations such as cloning a category on the configured
/// server. The service should be stopped gracefully to ensure the Discord client disconnects cleanly.</remarks>
public sealed class DiscordHostedService : IHostedService
{
    private readonly ILogger<DiscordHostedService> _log;
    private readonly DiscordSocketClient _client;
    private readonly DiscordOptions _opt;
    private readonly Prompt _prompt;
    private readonly CategoryCloner _cloner;
    private readonly CleanupService _cleanup;
    private readonly VerificationService _verification;
    private TaskCompletionSource<bool>? _readyTcs;

    public DiscordHostedService(
        ILogger<DiscordHostedService> log,
        DiscordSocketClient client,
        IOptions<DiscordOptions> opt,
        Prompt prompt,
        CategoryCloner cloner,
        CleanupService cleanup,
        VerificationService verification)
    {
        _log = log;
        _client = client;
        _opt = opt.Value;
        _prompt = prompt;
        _cloner = cloner;
        _cleanup = cleanup;
        _verification = verification;
    }

    /// <summary>
    /// Asynchronously logs in to the Discord client, waits for the client to become ready, and initiates the cloning of
    /// a server category based on user input.
    /// </summary>
    /// <remarks>This method must be called before performing operations that require the Discord client to be
    /// connected and ready. If the specified server cannot be found, the method logs an error and completes without
    /// performing the clone operation. The method does not throw if the operation is canceled; cancellation is
    /// cooperative via the provided token.</remarks>
    /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    public async Task StartAsync(CancellationToken ct)
    {
        _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.Ready += OnReady;

        await _client.LoginAsync(TokenType.Bot, _opt.Token);
        await _client.StartAsync();

        await _readyTcs.Task; // wait for Ready

        var server = _client.GetGuild(_opt.ServerId);
        if (server == null)
        {
            _log.LogError("❌ Server {ServerId} not found.", _opt.ServerId);
            return;
        }

        var newCategoryName = await _prompt.GetNewCategoryNameAsync();
        
        if (_opt.TestMode)
        {
            _log.LogInformation("🧪 Running in TEST MODE - resources will be tracked for cleanup");
            _log.LogInformation("🔍 Debug: TestMode={TestMode}, AutoCleanup={AutoCleanup}", _opt.TestMode, _opt.AutoCleanup);
            var createdResources = await _cloner.CloneWithTrackingAsync(server, _opt.SourceCategoryName, newCategoryName, _opt);
            
            if (createdResources != null)
            {
                _log.LogInformation("✅ Test mode: Resources created successfully!");
                _log.LogInformation("   📁 Category: {CategoryId}", createdResources.CategoryId);
                _log.LogInformation("   📺 Channels: {ChannelCount}", createdResources.Channels.Count);
                if (createdResources.RoleId.HasValue)
                    _log.LogInformation("   🧩 Role: {RoleId}", createdResources.RoleId);
                
                // Run verification
                _log.LogInformation("🔍 Running post-creation verification...");
                var verificationResult = await _verification.VerifyAsync(server, createdResources, _opt);
                LogVerificationResults(verificationResult);
                
                Console.WriteLine();
                Console.WriteLine("🔍 Please verify the created resources in Discord, then press ENTER to continue...");
                
                // Check if running in automated mode
                var isAutomated = _opt.AutoCleanup || 
                                 Console.IsInputRedirected || 
                                 Environment.GetEnvironmentVariable("DISCORD_ARCHITECT_AUTO") == "true" ||
                                 _opt.TestMode && Environment.GetEnvironmentVariable("CI") == "true";
                
                _log.LogInformation("🔍 Debug: AutoCleanup={AutoCleanup}, IsInputRedirected={IsInputRedirected}, DISCORD_ARCHITECT_AUTO={DiscordAuto}, CI={CI}, isAutomated={IsAutomated}", 
                    _opt.AutoCleanup, Console.IsInputRedirected, Environment.GetEnvironmentVariable("DISCORD_ARCHITECT_AUTO"), Environment.GetEnvironmentVariable("CI"), isAutomated);
                
                if (isAutomated)
                {
                    _log.LogInformation("🤖 Automated mode detected - proceeding with automatic cleanup");
                    await Task.Delay(2000); // Give a moment to see the logs
                }
                else
                {
                    _log.LogInformation("👤 Manual mode - waiting for user input");
                    Console.ReadLine();
                }
                
                Console.WriteLine();
                Console.Write("🗑️  Do you want to delete the created resources? (y/n): ");
                
                string response;
                if (isAutomated)
                {
                    response = "y"; // Automatically delete in automated mode
                    _log.LogInformation("🤖 Automated mode - automatically deleting resources");
                }
                else
                {
                    response = Console.ReadLine() ?? "n";
                }
                
                if (response?.ToLowerInvariant().StartsWith("y") == true)
                {
                    _log.LogInformation("🧹 Starting cleanup...");
                    await _cleanup.CleanupAsync(server, createdResources);
                }
                else
                {
                    _log.LogInformation("✅ Test mode completed - resources left intact");
                }
            }
        }
        else
        {
            var createdResources = await _cloner.CloneWithTrackingAsync(server, _opt.SourceCategoryName, newCategoryName, _opt);
            
            if (createdResources != null)
            {
                // Run verification for normal mode too
                _log.LogInformation("🔍 Running post-creation verification...");
                var verificationResult = await _verification.VerifyAsync(server, createdResources, _opt);
                LogVerificationResults(verificationResult);
            }
        }
        
        // Check if running in automated mode for final exit
        var isAutomatedExit = _opt.AutoCleanup || 
                             Console.IsInputRedirected || 
                             Environment.GetEnvironmentVariable("DISCORD_ARCHITECT_AUTO") == "true" ||
                             _opt.TestMode && Environment.GetEnvironmentVariable("CI") == "true";

        if (isAutomatedExit)
        {
            _log.LogInformation("✅ Done. Exiting automatically...");
            Environment.Exit(0);
        }
        else
        {
            _log.LogInformation("✅ Done. Press ENTER to exit…");
            Console.ReadLine();
        }
    }

    private void LogVerificationResults(VerificationResult result)
    {
        _log.LogInformation("📊 Verification Results:");
        
        foreach (var finding in result.Findings)
        {
            var icon = finding.Type switch
            {
                VerificationType.Success => "✅",
                VerificationType.Warning => "⚠️",
                VerificationType.Error => "❌",
                VerificationType.Info => "ℹ️",
                _ => "❓"
            };
            
            _log.LogInformation("  {Icon} [{Category}] {Message}", icon, finding.Category, finding.Message);
            if (!string.IsNullOrEmpty(finding.Description))
            {
                _log.LogInformation("     {Description}", finding.Description);
            }
        }
        
        if (result.Recommendations.Any())
        {
            _log.LogInformation("💡 Recommendations:");
            foreach (var recommendation in result.Recommendations)
            {
                _log.LogInformation("  • {Recommendation}", recommendation);
            }
        }
        
        _log.LogInformation("📋 {Summary}", result.Summary);
    }

    /// <summary>
    /// Asynchronously stops the client and logs out, releasing any associated resources.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to cancel the stop operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopAsync(CancellationToken ct)
    {
        _client.Ready -= OnReady;
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    /// <summary>
    /// Handles actions to perform when the gateway connection is ready.
    /// </summary>
    /// <returns>A completed task that represents the asynchronous operation.</returns>
    private Task OnReady()
    {
        _log.LogInformation("✅ Gateway Ready as {User}", _client.CurrentUser.Username);
        _readyTcs?.TrySetResult(true);
        return Task.CompletedTask;
    }
}
