using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace SierraSam.Tests.Integration;

internal static class DbContainerFactory
{
    public static IContainer CreateMsSqlContainer(string password, int portBinding = 1433)
    {
        return new ContainerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPortBinding(portBinding, 1433)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", password)
            .WithWaitStrategy
            (Wait
                .ForUnixContainer()
                .UntilCommandIsCompleted
                    ("/opt/mssql-tools/bin/sqlcmd",
                     "-S", $"127.0.0.1,{portBinding}",
                     "-U", "sa",
                     "-P", password))
            .Build();
    }

    public static IContainer CreatePostgresContainer(string password, int portBinding = 5432)
    {
        return new ContainerBuilder()
            .WithImage("postgres:latest")
            .WithPortBinding(portBinding, 5432)
            .WithEnvironment("POSTGRES_USER", "sa")
            .WithEnvironment("POSTGRES_PASSWORD", password)
            .WithWaitStrategy(Wait
                .ForUnixContainer()
                .UntilPortIsAvailable(5432))
            .Build();
    }
}