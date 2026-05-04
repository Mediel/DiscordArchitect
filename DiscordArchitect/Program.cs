using DiscordArchitect.Configuration;
using DiscordArchitect.Logging;
using DiscordArchitect.Options;
using DiscordArchitect.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// Configure Serilog early based on command line args
var tempConfig = DiscordArchitect.Configuration.ConfigurationBuilder.ParseCommandLineArgs(args);
var tempOptions = OptionsBuilder.BuildEarlyDiscordOptions(tempConfig);

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
            var earlyJsonOutput = DiscordArchitect.Configuration.ConfigurationBuilder.ParseBoolean(ctx.Configuration, "Discord:JsonOutput", "json", "--json");

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
        var verbose = DiscordArchitect.Configuration.ConfigurationBuilder.ParseBoolean(ctx.Configuration, "Discord:Verbose", "verbose", "--verbose");
        var jsonOutput = DiscordArchitect.Configuration.ConfigurationBuilder.ParseBoolean(ctx.Configuration, "Discord:JsonOutput", "json", "--json");

        if (!jsonOutput)
        {
            Console.WriteLine("✅ Configuration validation passed.");
            var sourceTemplate = ctx.Configuration["Discord:SourceCategoryName"] ?? "Template";
            var sid = ctx.Configuration["Discord:ServerId"] ?? "";
            Console.WriteLine($"   Server ID: {sid}   ·   Template category: {sourceTemplate}");
            Console.WriteLine();
        }
        else
        {
            Log.Information("Configuration validation passed");
        }
        
        // Build Discord options
        var options = OptionsBuilder.BuildDiscordOptions(ctx.Configuration, verbose);
        
        // Configure services
        ServiceConfiguration.ConfigureServices(services, ctx.Configuration, options);
    })
    .UseSerilog((context, services, configuration) =>
    {
        var options = services.GetRequiredService<DiscordOptions>();
        var minimumLevel = options.Verbose ? LogEventLevel.Debug : LogEventLevel.Information;

        configuration
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "DiscordArchitect")
            .Enrich.WithProperty("Version", "1.0.0")
            .WithDiscordArchitectLevels(options);

        if (options.JsonOutput)
        {
            configuration.WriteTo.Console(
                new JsonFormatter(renderMessage: true),
                restrictedToMinimumLevel: minimumLevel);
        }
        else
        {
            configuration.WriteTo.Console(
                outputTemplate: SerilogConfiguration.HumanConsoleTemplate,
                restrictedToMinimumLevel: minimumLevel);
        }

        configuration.WriteTo.File(
            path: "logs/discord-architect-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: options.JsonOutput
                ? "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                : SerilogConfiguration.HumanFileTemplate,
            restrictedToMinimumLevel: LogEventLevel.Information);
    });

await builder.RunConsoleAsync();
