using System.Net;
using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace SierraSam.Tests.Integration;

internal static class DbContainerFactory
{
    public static IContainer CreateMsSqlContainer(string password)
    {
        return new ContainerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPortBinding(1433, true)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", password)
            .WithWaitStrategy
            (Wait
                .ForUnixContainer()
                .UntilCommandIsCompleted
                ("/opt/mssql-tools/bin/sqlcmd",
                    "-S", $"127.0.0.1,{1433}",
                    "-U", "sa",
                    "-P", password)
            )
            .Build();
    }

    public static IContainer CreatePostgresContainer(string password)
    {
        return new ContainerBuilder()
            .WithImage("postgres:latest")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_USER", "sa")
            .WithEnvironment("POSTGRES_PASSWORD", password)
            .WithWaitStrategy(Wait
                .ForUnixContainer()
                .UntilPortIsAvailable(5432))
            .Build();
    }

    public static IContainer CreateMysqlContainer(string password)
    {
        return new ContainerBuilder()
            .WithImage("mysql:latest")
            .WithPortBinding(3306, true)
            .WithEnvironment("MYSQL_ROOT_PASSWORD", password)
            .WithEnvironment("MYSQL_DATABASE", "test")
            .WithWaitStrategy(Wait
                .ForUnixContainer()
                .UntilPortIsAvailable(3306))
            .Build();
    }

    /// <summary>
    /// Let the OS assign the next available port. Unless we cycle through all ports
    /// on a test run, the OS will always increment the port number when making these calls.
    /// This prevents races in parallel test runs where a test is already bound to
    /// a given port, and a new test is able to bind to the same port due to port
    /// reuse being enabled by default by the OS.
    /// </summary>
    /// <see cref="https://github.com/dotnet/tye/blob/main/src/Microsoft.Tye.Core/NextPortFinder.cs"/>
    private static int GetPort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}