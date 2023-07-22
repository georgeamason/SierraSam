
using System.Data.Odbc;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Factories;
using SierraSam.Database;
using Version = SierraSam.Capabilities.Version;

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
            .ConfigureLogging((_, builder) =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss]";
                    options.UseUtcTimestamp = true;
                });

                //builder.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<App>();

                services.AddSingleton<OdbcConnection>
                    (s => OdbcConnectionFactory.Create
                        (s.GetRequiredService<ILogger<App>>(),
                         s.GetRequiredService<Configuration>()));

                services.AddSingleton<Configuration>
                    (s => new ConfigurationFactory
                        (s.GetRequiredService<ILogger<ConfigurationFactory>>(),
                         s.GetRequiredService<IFileSystem>(),
                         ConfigPaths())
                            .Create(args));

                services.AddSingleton<IDatabase>
                    (s => DatabaseFactory.Create
                        (s.GetRequiredService<OdbcConnection>(),
                         s.GetRequiredService<Configuration>()));

                services.AddSingleton<IFileSystem, FileSystem>();

                services.AddSingleton<ICapabilityResolver, CapabilityResolver>();
                services.AddSingleton<ICapability, Version>();
                services.AddSingleton<ICapability, Help>();
                services.AddSingleton<ICapability, Migrate>();
                services.AddSingleton<ICapability, Auth>();
                services.AddSingleton<ICapability, Clean>();
                services.AddSingleton<ICapability, Baseline>();
            })
            .Build();

        var app = host.Services.GetRequiredService<App>();

        try
        {
            app.Start(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occured: {ex.Message}");
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