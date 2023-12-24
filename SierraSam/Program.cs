using System.Data;
using System.IO.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationApplicators;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;
using SierraSam.Core.Serializers;
using SierraSam.Database;
using Spectre.Console;
using Version = SierraSam.Capabilities.Version;
using IConfiguration = SierraSam.Core.IConfiguration;

namespace SierraSam;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            using var host = Host
                .CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                {
                    // Switch mappings can go here if required
                    builder.AddCommandLine(args);
                })
                .ConfigureLogging((ctx, builder) =>
                {
                    builder.ClearProviders();

                    builder.SetMinimumLevel(
                        ctx.HostingEnvironment.IsDevelopment() ? LogLevel.Trace : LogLevel.Information
                    );

                    builder.AddSimpleConsole(options =>
                    {
                        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] => ";
                        options.UseUtcTimestamp = true;
                        options.SingleLine = true;
                    });
                })
                // TODO: Extract into a separate class
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<App>();

                    services.AddSingleton<IDbConnection>(
                        s => OdbcConnectionFactory.Create(
                            s.GetRequiredService<ILogger<App>>(),
                            s.GetRequiredService<IConfiguration>())
                    );

                    services.AddSingleton<IConfiguration>(
                        s => ConfigurationFactory.Create(
                            s.GetRequiredService<ILoggerFactory>(),
                            s.GetRequiredService<IFileSystem>(),
                            args)
                    );

                    services.AddSingleton<IDbExecutor, DbExecutor>();

                    services.AddMemoryCache();

                    services.AddSingleton<IDatabase>(
                        s => DatabaseResolver.Create(
                            s.GetRequiredService<ILoggerFactory>(),
                            s.GetRequiredService<IDbConnection>(),
                            s.GetRequiredService<IDbExecutor>(),
                            s.GetRequiredService<IConfiguration>(),
                            s.GetRequiredService<IMemoryCache>())
                    );

                    services.AddSingleton<IFileSystem, FileSystem>();

                    services.AddSingleton<IMigrationSeeker>(
                        s => MigrationSeekerFactory.Create(
                            s.GetRequiredService<IConfiguration>(),
                            s.GetRequiredService<IFileSystem>())
                    );

                    services.AddSingleton<IMigrationsApplicator, MigrationsApplicator>();

                    services.AddSingleton<IMigrationApplicatorResolver, MigrationApplicatorResolver>();
                    services.AddSingleton<IMigrationApplicator, VersionedMigrationApplicator>();
                    services.AddSingleton<IMigrationApplicator, RepeatableMigrationApplicator>();

                    services.AddSingleton<IMigrationValidator>(
                        s => MigrationValidatorFactory.Create(
                            s.GetRequiredService<IMigrationSeeker>(),
                            s.GetRequiredService<IDatabase>(),
                            s.GetRequiredService<IIgnoredMigrationsFactory>())
                    );

                    services.AddSingleton<IMigrationMerger, MigrationMerger>();
                    services.AddSingleton<IIgnoredMigrationsFactory, IgnoredMigrationsFactory>();

                    services.AddSingleton<ICapabilityResolver, CapabilityResolver>();
                    services.AddSingleton<ICapability, Auth>();
                    services.AddSingleton<ICapability, Rollup>();
                    services.AddSingleton<ICapability, Clean>();
                    services.AddSingleton<ICapability, Help>();
                    services.AddSingleton<ICapability, Information>();
                    services.AddSingleton<ICapability, Initialise>();
                    services.AddSingleton<ICapability, Migrate>();
                    services.AddSingleton<ICapability, Validate>();
                    services.AddSingleton<ICapability, Version>();

                    services.AddSingleton<ISerializer>(
                        s => SerializerFactory.Create(
                            s.GetRequiredService<IConfiguration>())
                    );

                    services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);
                })
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<App>>();
            var app = host.Services.GetRequiredService<App>();
            var console = host.Services.GetRequiredService<IAnsiConsole>();

            try
            {
                logger.LogDebug("Starting app...");
                app.Start(args);
                logger.LogDebug("Terminating app...");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "{exception}", exception.Message);
                console.MarkupLineInterpolated($"[red]{exception.Message}[/]");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
        }
    }
}