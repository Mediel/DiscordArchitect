using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DiscordArchitect.Options;
using Serilog;

namespace DiscordArchitect.Logging;

/// <summary>
/// Provides structured logging functionality with support for human-readable and JSON output formats using Serilog.
/// </summary>
public sealed class StructuredLogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly DiscordOptions _options;

    public StructuredLogger(Microsoft.Extensions.Logging.ILogger<StructuredLogger> logger, IOptions<DiscordOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Logs an information message with structured data.
    /// </summary>
    public void LogInfo(EventId eventId, string message, params object[] args)
    {
        if (_options.JsonOutput)
        {
            _logger.LogInformation(eventId, message, args);
        }
        else
        {
            _logger.LogInformation(eventId, message, args);
        }
    }

    /// <summary>
    /// Logs a verbose message with structured data (only when verbose mode is enabled).
    /// </summary>
    public void LogVerbose(EventId eventId, string message, params object[] args)
    {
        if (!_options.Verbose) return;

        if (_options.JsonOutput)
        {
            _logger.LogDebug(eventId, message, args);
        }
        else
        {
            _logger.LogDebug(eventId, message, args);
        }
    }

    /// <summary>
    /// Logs a warning message with structured data.
    /// </summary>
    public void LogWarning(EventId eventId, string message, params object[] args)
    {
        if (_options.JsonOutput)
        {
            _logger.LogWarning(eventId, message, args);
        }
        else
        {
            _logger.LogWarning(eventId, message, args);
        }
    }

    /// <summary>
    /// Logs an error message with structured data.
    /// </summary>
    public void LogError(EventId eventId, string message, params object[] args)
    {
        if (_options.JsonOutput)
        {
            _logger.LogError(eventId, message, args);
        }
        else
        {
            _logger.LogError(eventId, message, args);
        }
    }

    /// <summary>
    /// Logs a success message with emoji (human mode only).
    /// </summary>
    public void LogSuccess(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"✅ {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a warning message with emoji (human mode only).
    /// </summary>
    public void LogWarningWithEmoji(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"⚠️ {message}";
        LogWarning(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs an error message with emoji (human mode only).
    /// </summary>
    public void LogErrorWithEmoji(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"❌ {message}";
        LogError(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs an info message with emoji (human mode only).
    /// </summary>
    public void LogInfoWithEmoji(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"ℹ️ {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a diagnostic message with emoji (human mode only).
    /// </summary>
    public void LogDiagnostic(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"🔍 {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a test mode message with emoji (human mode only).
    /// </summary>
    public void LogTestMode(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"🧪 {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a cleanup message with emoji (human mode only).
    /// </summary>
    public void LogCleanup(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"🧹 {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a verification message with emoji (human mode only).
    /// </summary>
    public void LogVerification(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"🔍 {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a category message with emoji (human mode only).
    /// </summary>
    public void LogCategory(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"📁 {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a channel message with emoji (human mode only).
    /// </summary>
    public void LogChannel(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"📺 {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a role message with emoji (human mode only).
    /// </summary>
    public void LogRole(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"🧩 {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a forum message with emoji (human mode only).
    /// </summary>
    public void LogForum(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"🗂️ {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a permission message with emoji (human mode only).
    /// </summary>
    public void LogPermission(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"🔒 {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a summary message with emoji (human mode only).
    /// </summary>
    public void LogSummary(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"📋 {message}";
        LogInfo(eventId, formattedMessage, args);
    }

    /// <summary>
    /// Logs a recommendation message with emoji (human mode only).
    /// </summary>
    public void LogRecommendation(EventId eventId, string message, params object[] args)
    {
        var formattedMessage = _options.JsonOutput ? message : $"💡 {message}";
        LogInfo(eventId, formattedMessage, args);
    }
}
