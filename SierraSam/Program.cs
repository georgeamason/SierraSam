
using System.Data.Odbc;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Factories;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;
using SierraSam.Database;
using Version = SierraSam.Capabilities.Version;
using Console = SierraSam.Core.ColorConsole;

namespace SierraSam;

public static class Program
{
    public static void Main(string[] args)
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

                builder.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] => ";
                    options.UseUtcTimestamp = true;
                    options.SingleLine      = true;
                });

                if (ctx.HostingEnvironment.IsDevelopment())
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                }

                builder.AddDebug();
            })
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<App>();

                services.AddSingleton<OdbcConnection>
                    (s => OdbcConnectionFactory.Create
                        (s.GetRequiredService<ILogger<App>>(),
                         s.GetRequiredService<Configuration>()));

                services.AddSingleton<Configuration>
                    (s => ConfigurationFactory.Create
                        (s.GetRequiredService<ILoggerFactory>(),
                         s.GetRequiredService<IFileSystem>(),
                         ConfigPaths(),
                         args));

                services.AddSingleton<IDatabase>
                    (s => DatabaseResolver.Create
                        (s.GetRequiredService<OdbcConnection>(),
                         s.GetRequiredService<Configuration>()));

                services.AddSingleton<IFileSystem, FileSystem>();

                services.AddSingleton<IMigrationSeeker>
                    (s => MigrationSeekerFactory.Create
                        (s.GetRequiredService<Configuration>(),
                         s.GetRequiredService<IFileSystem>()));

                services.AddSingleton<IMigrationApplicator, MigrationApplicator>();

                services.AddSingleton<IMigrationValidator>
                    (s => MigrationValidatorFactory.Create
                        (s.GetRequiredService<Configuration>()));

                services.AddSingleton<IMigrationMerger, MigrationMerger>();

                services.AddSingleton<ICapabilityResolver, CapabilityResolver>();
                services.AddSingleton<ICapability, Auth>();
                services.AddSingleton<ICapability, Baseline>();
                services.AddSingleton<ICapability, Clean>();
                services.AddSingleton<ICapability, Help>();
                services.AddSingleton<ICapability, Information>();
                services.AddSingleton<ICapability, Migrate>();
                services.AddSingleton<ICapability, Validate>();
                services.AddSingleton<ICapability, Version>();
            })
            .Build();

            var logger = host.Services.GetRequiredService<ILogger<App>>();
            var app = host.Services.GetRequiredService<App>();

            try
            {
                logger.LogDebug("Starting app...");
                app.Start(args);
                logger.LogDebug("Terminating app...");
            }
            catch (Exception exception)
            {
                // logger.LogError(exception, exception.Message);
                Console.ErrorLine($"{exception.Message}");
            }
    }

    private static IEnumerable<string> ConfigPaths(string fileName = "flyway.json")
    {
        var userFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return new []
        {
            Path.Combine(userFolderPath, fileName),
            Path.Combine(Environment.CurrentDirectory, fileName)
        };
    }
}