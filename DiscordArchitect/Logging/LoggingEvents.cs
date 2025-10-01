using Microsoft.Extensions.Logging;

namespace DiscordArchitect.Logging;

/// <summary>
/// Defines structured logging events with consistent event IDs for the DiscordArchitect application.
/// </summary>
public static class LoggingEvents
{
    // Application lifecycle events (1000-1099)
    public static readonly EventId ApplicationStarting = new(1001, "ApplicationStarting");
    public static readonly EventId ApplicationStarted = new(1002, "ApplicationStarted");
    public static readonly EventId ApplicationStopping = new(1003, "ApplicationStopping");
    public static readonly EventId ApplicationStopped = new(1004, "ApplicationStopped");

    // Configuration events (1100-1199)
    public static readonly EventId ConfigurationValidationStarted = new(1101, "ConfigurationValidationStarted");
    public static readonly EventId ConfigurationValidationPassed = new(1102, "ConfigurationValidationPassed");
    public static readonly EventId ConfigurationValidationFailed = new(1103, "ConfigurationValidationFailed");
    public static readonly EventId ConfigurationLoaded = new(1104, "ConfigurationLoaded");

    // Discord connection events (1200-1299)
    public static readonly EventId DiscordConnecting = new(1201, "DiscordConnecting");
    public static readonly EventId DiscordConnected = new(1202, "DiscordConnected");
    public static readonly EventId DiscordDisconnected = new(1203, "DiscordDisconnected");
    public static readonly EventId DiscordReady = new(1204, "DiscordReady");
    public static readonly EventId DiscordLoginFailed = new(1205, "DiscordLoginFailed");

    // Guild operations (1300-1399)
    public static readonly EventId GuildFound = new(1301, "GuildFound");
    public static readonly EventId GuildNotFound = new(1302, "GuildNotFound");
    public static readonly EventId GuildPermissionsChecked = new(1303, "GuildPermissionsChecked");

    // Category operations (1400-1499)
    public static readonly EventId CategorySearchStarted = new(1401, "CategorySearchStarted");
    public static readonly EventId CategoryFound = new(1402, "CategoryFound");
    public static readonly EventId CategoryNotFound = new(1403, "CategoryNotFound");
    public static readonly EventId CategoryCreated = new(1404, "CategoryCreated");
    public static readonly EventId CategoryDeleted = new(1405, "CategoryDeleted");

    // Channel operations (1500-1599)
    public static readonly EventId ChannelCreated = new(1501, "ChannelCreated");
    public static readonly EventId ChannelDeleted = new(1501, "ChannelDeleted");
    public static readonly EventId ChannelSynced = new(1503, "ChannelSynced");
    public static readonly EventId ChannelPermissionsApplied = new(1504, "ChannelPermissionsApplied");

    // Role operations (1600-1699)
    public static readonly EventId RoleCreated = new(1601, "RoleCreated");
    public static readonly EventId RoleDeleted = new(1602, "RoleDeleted");
    public static readonly EventId RolePermissionsApplied = new(1603, "RolePermissionsApplied");

    // Forum operations (1700-1799)
    public static readonly EventId ForumTagsPatching = new(1701, "ForumTagsPatching");
    public static readonly EventId ForumTagsPatched = new(1702, "ForumTagsPatched");
    public static readonly EventId ForumTagsPatchFailed = new(1703, "ForumTagsPatchFailed");

    // Test mode operations (1800-1899)
    public static readonly EventId TestModeStarted = new(1801, "TestModeStarted");
    public static readonly EventId TestModeCompleted = new(1802, "TestModeCompleted");
    public static readonly EventId TestModeCleanupStarted = new(1803, "TestModeCleanupStarted");
    public static readonly EventId TestModeCleanupCompleted = new(1804, "TestModeCleanupCompleted");

    // Verification operations (1900-1999)
    public static readonly EventId VerificationStarted = new(1901, "VerificationStarted");
    public static readonly EventId VerificationCompleted = new(1902, "VerificationCompleted");
    public static readonly EventId VerificationFinding = new(1903, "VerificationFinding");
    public static readonly EventId VerificationSummary = new(1904, "VerificationSummary");

    // Error events (2000-2099)
    public static readonly EventId UnexpectedError = new(2001, "UnexpectedError");
    public static readonly EventId ValidationError = new(2002, "ValidationError");
    public static readonly EventId DiscordApiError = new(2003, "DiscordApiError");
    public static readonly EventId NetworkError = new(2004, "NetworkError");

    // Warning events (2100-2199)
    public static readonly EventId PermissionWarning = new(2101, "PermissionWarning");
    public static readonly EventId ConfigurationWarning = new(2102, "ConfigurationWarning");
    public static readonly EventId ResourceWarning = new(2103, "ResourceWarning");

    // Info events (2200-2299)
    public static readonly EventId OperationInfo = new(2201, "OperationInfo");
    public static readonly EventId DiagnosticInfo = new(2202, "DiagnosticInfo");
    public static readonly EventId PerformanceInfo = new(2203, "PerformanceInfo");
}
