using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace SierraSam.Tests.Integration;

internal static class DbContainerFactory
{
    private const string Password = "yourStrong(!)Password";

    public interface IDbContainer
    {
        public string ConnectionString { get; }

        public Task StartAsync();

        public Task StopAsync();
    }

    internal sealed class SqlServer : IDbContainer
    {
        private readonly IContainer _container = Create();

        private static IContainer Create()
        {
            return new ContainerBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPortBinding(1433, true)
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("MSSQL_SA_PASSWORD", Password)
                .WithWaitStrategy(
                    Wait
                        .ForUnixContainer()
                        .UntilCommandIsCompleted
                        ("/opt/mssql-tools/bin/sqlcmd",
                            "-S", $"127.0.0.1,{1433}",
                            "-U", "sa",
                            "-P", Password)
                )
                .Build();
        }

        public string ConnectionString => $"Driver={{ODBC Driver 17 for SQL Server}};" +
                                          $"Server=127.0.0.1,{_container.GetMappedPublicPort(1433)};" +
                                          $"UID=sa;PWD={Password};";

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();
    }

    internal sealed class Postgres : IDbContainer
    {
        private readonly IContainer _container = Create();

        private static IContainer Create()
        {
            return new ContainerBuilder()
                .WithImage("postgres:latest")
                .WithPortBinding(5432, true)
                .WithEnvironment("POSTGRES_USER", "sa")
                .WithEnvironment("POSTGRES_PASSWORD", Password)
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilPortIsAvailable(5432))
                .Build();
        }

        public string ConnectionString => $"Driver={{PostgreSQL UNICODE}};" +
                                          $"Server=127.0.0.1;Port={_container.GetMappedPublicPort(5432)};" +
                                          $"Uid=sa;Pwd={Password};";

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();
    }

    internal sealed class MySql : IDbContainer
    {
        private readonly IContainer _container = Create();

        private static IContainer Create()
        {
            return new ContainerBuilder()
                .WithImage("mysql:latest")
                .WithPortBinding(3306, true)
                .WithEnvironment("MYSQL_ROOT_PASSWORD", Password)
                .WithEnvironment("MYSQL_DATABASE", "test")
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilPortIsAvailable(3306))
                .Build();
        }

        public string ConnectionString => $"Driver={{MySQL ODBC 8.2 UNICODE Driver}};" +
                                          $"Server=127.0.0.1;Port={_container.GetMappedPublicPort(3306)};" +
                                          $"Database=test;" +
                                          $"User=root;Password={Password};";

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();
    }
}