using DiscordArchitect.DiscordFactories;
using DiscordArchitect.Hosting;
using DiscordArchitect.Options;
using DiscordArchitect.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            Console.WriteLine("❌ Configuration validation failed:");
            Console.WriteLine(validationResult.ErrorMessage);
            Console.WriteLine();
            Console.WriteLine("Please fix the configuration issues and try again.");
            Environment.Exit(1);
        }
        
        Console.WriteLine("✅ Configuration validation passed.");
        // Options binding (bez Binderu, ale použijeme standardní pattern)
        var opts = new DiscordOptions
        {
            Token = ctx.Configuration["Discord:Token"] ?? string.Empty,
            SourceCategoryName = ctx.Configuration["Discord:SourceCategoryName"] ?? "Template",
            CreateRolePerCategory = bool.TryParse(ctx.Configuration["Discord:CreateRolePerCategory"], out var c1) && c1,
            EveryoneAccessToNewCategory = bool.TryParse(ctx.Configuration["Discord:EveryoneAccessToNewCategory"], out var c2) && c2,
            SyncChannelsToCategory = bool.TryParse(ctx.Configuration["Discord:SyncChannelsToCategory"], out var c3) && c3,
            TestMode = bool.TryParse(ctx.Configuration["Discord:TestMode"], out var c4) && c4 || 
                      bool.TryParse(ctx.Configuration["test-mode"], out var c5) && c5 ||
                      bool.TryParse(ctx.Configuration["TestMode"], out var c6) && c6
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
    .ConfigureLogging(log =>
    {
        log.ClearProviders();
        log.AddSimpleConsole(o =>
        {
            o.TimestampFormat = "HH:mm:ss ";
            o.SingleLine = true;
        });
        log.SetMinimumLevel(LogLevel.Information);
    });

await builder.RunConsoleAsync();
