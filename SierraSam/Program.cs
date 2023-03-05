
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedGate.Client.Activation.Shim;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Core.Factories;
using SierraSam.Core.Providers;
using SierraSam.Licensing;
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
            })
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<App>();

                services.AddSingleton<Configuration>
                    (s => new ConfigurationFactory
                        (s.GetRequiredService<ILogger<ConfigurationFactory>>(),
                         s.GetRequiredService<IFileSystemProvider>(),
                         ConfigPaths())
                            .Create(args));

                services.AddSingleton<IFileSystemProvider, FileSystemProvider>();

                services.AddSingleton<ILicenseClient>
                (s => LicenseClientFactory.Create(
                    s.GetRequiredService<ILogger<App>>()));

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