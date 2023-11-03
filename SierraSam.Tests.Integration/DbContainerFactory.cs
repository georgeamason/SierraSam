using System.Data.Common;
using System.Data.Odbc;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Respawn;

namespace SierraSam.Tests.Integration;

internal static class DbContainerFactory
{
    private const string Password = "yourStrong(!)Password";

    public interface IDbContainer
    {
        public DbConnection DbConnection { get; }
        public TestcontainersStates State { get; }
        public IDbAdapter Adapter { get; }
        public Task StartAsync();
        public Task StopAsync();
    }

    internal sealed class SqlServer : IDbContainer
    {
        private readonly IContainer _container;

        public SqlServer(string tag = "latest")
        {
            _container = new ContainerBuilder()
                .WithImage($"mcr.microsoft.com/mssql/server:{tag}")
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

        public DbConnection DbConnection
        {
            get
            {
                if (_container.State is not TestcontainersStates.Running)
                {
                    throw new Exception("Container must be running to get connection string");
                }

                return new OdbcConnection(
                    $"Driver={{ODBC Driver 17 for SQL Server}};" +
                    $"Server=127.0.0.1,{_container.GetMappedPublicPort(1433)};" +
                    $"UID=sa;PWD={Password};"
                );
            }
        }

        public TestcontainersStates State => _container.State;

        public IDbAdapter Adapter => DbAdapter.SqlServer;

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();
    }

    internal sealed class Postgres : IDbContainer
    {
        private readonly IContainer _container;

        public Postgres(string tag = "latest")
        {
            _container = new ContainerBuilder()
                .WithImage($"postgres:{tag}")
                .WithPortBinding(5432, true)
                .WithEnvironment("POSTGRES_USER", "sa")
                .WithEnvironment("POSTGRES_PASSWORD", Password)
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilPortIsAvailable(5432))
                .Build();
        }

        public DbConnection DbConnection
        {
            get
            {
                if (_container.State is not TestcontainersStates.Running)
                {
                    throw new Exception("Container must be running to get connection string");
                }

                return new OdbcConnection(
                    $"Driver={{PostgreSQL UNICODE}};" +
                    $"Server=127.0.0.1;Port={_container.GetMappedPublicPort(5432)};" +
                    $"Uid=sa;Pwd={Password};"
                );
            }
        }

        public TestcontainersStates State => _container.State;

        public IDbAdapter Adapter => DbAdapter.Postgres;

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();
    }

    internal sealed class MySql : IDbContainer
    {
        private readonly IContainer _container;

        public MySql(string tag = "latest")
        {
            _container = new ContainerBuilder()
                .WithImage($"mysql:{tag}")
                .WithPortBinding(3306, true)
                .WithEnvironment("MYSQL_ROOT_PASSWORD", Password)
                .WithEnvironment("MYSQL_DATABASE", "test")
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilPortIsAvailable(3306))
                .Build();
        }

        public DbConnection DbConnection
        {
            get
            {
                if (_container.State is not TestcontainersStates.Running)
                {
                    throw new Exception("Container must be running to get connection string");
                }

                return new OdbcConnection(
                    $"Driver={{MySQL ODBC 8.2 UNICODE Driver}};" +
                    $"Server=127.0.0.1;Port={_container.GetMappedPublicPort(3306)};" +
                    $"Database=test;" +
                    $"User=root;Password={Password};" +
                    $"MULTI_STATEMENTS=1;"
                );
            }
        }

        public TestcontainersStates State => _container.State;

        public IDbAdapter Adapter => DbAdapter.MySql;

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();
    }
}