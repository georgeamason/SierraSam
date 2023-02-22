
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SierraSam.Capabilities;

using Version = SierraSam.Capabilities.Version;

namespace SierraSam;

public static class Program
{
    public static void Main(string[] args)
    {
        using var host = Host
            .CreateDefaultBuilder(args)
            // .ConfigureLogging((_, builder) =>
            // {
            //     builder.AddSimpleConsole(options =>
            //     {
            //         options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss]";
            //     });
            // })
            .ConfigureServices((_, services) =>
            {
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddSimpleConsole(options =>
                    {
                        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss]";
                        options.UseUtcTimestamp = true;
                    });
                });
                var logger = loggerFactory.CreateLogger(typeof(SierraSam.App));

                services.AddSingleton(logger);
                services.AddSingleton<App>();
                services.AddSingleton<ICapabilityFactory, CapabilityFactory>();
                services.AddSingleton<ICapability, Version>();
                services.AddSingleton<ICapability, Help>();

            })
            .Build();

        var app = host.Services.GetRequiredService<App>();
        app.Run(args);

        //host.Run();
    }
}