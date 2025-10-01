using DiscordArchitect.DiscordFactories;
using DiscordArchitect.Hosting;
using DiscordArchitect.Logging;
using DiscordArchitect.Options;
using DiscordArchitect.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// Configure Serilog early based on command line args
var tempConfig = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

var tempOptions = new DiscordOptions
{
    Verbose = bool.TryParse(tempConfig["Discord:Verbose"], out var tempV1) && tempV1 ||
             bool.TryParse(tempConfig["verbose"], out var tempV2) && tempV2 ||
             bool.TryParse(tempConfig["--verbose"], out var tempV3) && tempV3,
    JsonOutput = bool.TryParse(tempConfig["Discord:JsonOutput"], out var tempJ1) && tempJ1 ||
                bool.TryParse(tempConfig["json"], out var tempJ2) && tempJ2 ||
                bool.TryParse(tempConfig["--json"], out var tempJ3) && tempJ3
};

// Initialize Serilog early
Log.Logger = SerilogConfiguration.CreateLogger(tempOptions, tempConfig);

// Build Host
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
           .AddUserSecrets<Program>()
           .AddCommandLine(args);
    })
    .ConfigureServices((ctx, services) =>
    {
        // Validate configuration before proceeding
        var validator = new ConfigurationValidator(ctx.Configuration);
        var validationResult = validator.Validate();
        
        if (!validationResult.IsValid)
        {
        // Get logging options from configuration for early error handling
        var earlyJsonOutput = bool.TryParse(ctx.Configuration["Discord:JsonOutput"], out var ej1) && ej1 ||
                             bool.TryParse(ctx.Configuration["json"], out var ej2) && ej2 ||
                             bool.TryParse(ctx.Configuration["--json"], out var ej3) && ej3;

        if (!earlyJsonOutput)
        {
            Console.WriteLine("❌ Configuration validation failed:");
            Console.WriteLine(validationResult.ErrorMessage);
            Console.WriteLine();
            Console.WriteLine("Please fix the configuration issues and try again.");
        }
        else
        {
            Log.Error("Configuration validation failed: {ErrorMessage}", validationResult.ErrorMessage);
        }
            Environment.Exit(1);
        }
        
        // Get logging options from configuration
        var verbose = bool.TryParse(ctx.Configuration["Discord:Verbose"], out var v1) && v1 ||
                     bool.TryParse(ctx.Configuration["verbose"], out var v2) && v2 ||
                     bool.TryParse(ctx.Configuration["--verbose"], out var v3) && v3;
        var jsonOutput = bool.TryParse(ctx.Configuration["Discord:JsonOutput"], out var j1) && j1 ||
                        bool.TryParse(ctx.Configuration["json"], out var j2) && j2 ||
                        bool.TryParse(ctx.Configuration["--json"], out var j3) && j3;

        if (!jsonOutput)
        {
            Console.WriteLine("✅ Configuration validation passed.");
        }
        else
        {
            Log.Information("Configuration validation passed");
        }
        
        // Options binding (bez Binderu, ale použijeme standardní pattern)
        var autoCleanup = bool.TryParse(ctx.Configuration["Discord:AutoCleanup"], out var ac1) && ac1 ||
                         bool.TryParse(ctx.Configuration["auto-cleanup"], out var ac2) && ac2 ||
                         bool.TryParse(ctx.Configuration["--auto-cleanup"], out var ac3) && ac3 ||
                         bool.TryParse(ctx.Configuration["AutoCleanup"], out var ac4) && ac4 ||
                         Environment.GetEnvironmentVariable("DISCORD_ARCHITECT_AUTO_CLEANUP") == "true";
        
        var testMode = bool.TryParse(ctx.Configuration["Discord:TestMode"], out var t1) && t1 || 
                      bool.TryParse(ctx.Configuration["test-mode"], out var t2) && t2 ||
                      bool.TryParse(ctx.Configuration["TestMode"], out var t3) && t3;
        
        // FOR TESTING: Force test mode and auto cleanup when running from assistant
        if (Environment.GetEnvironmentVariable("DISCORD_ARCHITECT_AUTO_CLEANUP") == "true")
        {
            testMode = true;
            autoCleanup = true;
        }
        
        // Debug logging (only in verbose mode)
        if (verbose)
        {
            Console.WriteLine($"🔍 Debug: TestMode={testMode}, AutoCleanup={autoCleanup}");
        }
        
        var opts = new DiscordOptions
        {
            Token = ctx.Configuration["Discord:Token"] ?? string.Empty,
            SourceCategoryName = ctx.Configuration["Discord:SourceCategoryName"] ?? "Template",
            CreateRolePerCategory = bool.TryParse(ctx.Configuration["Discord:CreateRolePerCategory"], out var c1) && c1,
            EveryoneAccessToNewCategory = bool.TryParse(ctx.Configuration["Discord:EveryoneAccessToNewCategory"], out var c2) && c2,
            SyncChannelsToCategory = bool.TryParse(ctx.Configuration["Discord:SyncChannelsToCategory"], out var c3) && c3,
            TestMode = testMode,
            Verbose = verbose,
            JsonOutput = jsonOutput,
            AutoCleanup = autoCleanup
        };
        if (ulong.TryParse(ctx.Configuration["Discord:ServerId"], out var gid)) opts.ServerId = gid;

        services.AddSingleton(opts);
        services.AddOptions<DiscordOptions>().Configure(o =>
        {
            o.Token = opts.Token;
            o.ServerId = opts.ServerId;
            o.SourceCategoryName = opts.SourceCategoryName;
            o.CreateRolePerCategory = opts.CreateRolePerCategory;
            o.EveryoneAccessToNewCategory = opts.EveryoneAccessToNewCategory;
            o.SyncChannelsToCategory = opts.SyncChannelsToCategory;
            o.TestMode = opts.TestMode;
            o.Verbose = opts.Verbose;
            o.JsonOutput = opts.JsonOutput;
            o.AutoCleanup = opts.AutoCleanup;
        });

        // DiscordFactories client
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DiscordClient");
            return DiscordClientFactory.Create(logger);
        });

        // HttpClient for REST tags
        services.AddSingleton(sp =>
        {
            var o = sp.GetRequiredService<DiscordOptions>();
            return RestClientFactory.Create(o.Token);
        });

        // Services
        services.AddSingleton<Prompt>();
        services.AddSingleton<DiagnosticsService>();
        services.AddSingleton<PermissionPlanner>();
        services.AddSingleton<ForumTagService>();
        services.AddSingleton<CleanupService>();
        services.AddSingleton<VerificationService>();
        services.AddSingleton<CategoryCloner>();

        // Hosted service
        services.AddHostedService<DiscordHostedService>();
    })
    .UseSerilog((context, services, configuration) =>
    {
        // Get options from DI
        var options = services.GetRequiredService<DiscordOptions>();
        
        configuration
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "DiscordArchitect")
            .Enrich.WithProperty("Version", "1.0.0");

        // Set minimum level based on verbose mode
        var minimumLevel = options.Verbose ? LogEventLevel.Debug : LogEventLevel.Information;
        configuration.MinimumLevel.Is(minimumLevel);

        // Configure console output
        if (options.JsonOutput)
        {
            // JSON output for structured logging
            configuration.WriteTo.Console(
                new JsonFormatter(renderMessage: true),
                restrictedToMinimumLevel: minimumLevel);
        }
        else
        {
            // Human-readable output with emojis and colors
            configuration.WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: minimumLevel);
        }

        // Add file logging
        configuration.WriteTo.File(
            path: "logs/discord-architect-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: options.JsonOutput 
                ? "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                : "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            restrictedToMinimumLevel: LogEventLevel.Information);
    });

await builder.RunConsoleAsync();
