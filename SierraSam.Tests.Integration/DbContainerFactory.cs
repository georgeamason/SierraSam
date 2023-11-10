using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Respawn;

namespace SierraSam.Tests.Integration;

internal static class DbContainerFactory
{
    private const string Password = "yourStrong(!)Password";

    public static readonly IEnumerable<IDbContainer> ContainerTestCases = new IDbContainer[]
    {
        new SqlServer("2022-latest"),
        new SqlServer("2017-latest"),
        new Postgres("16"),
        new Postgres("15"),
        new MySql("8.2"),
        new MySql("5.7"),
        new Oracle("free", "23.3.0.0"),
        new Oracle("enterprise", "21.3.0.0"),
        new Oracle("enterprise", "19.3.0.0"),
    };

    public interface IDbContainer
    {
        public DbConnection DbConnection { get; }
        public Task StartAsync();
        public Task StopAsync();
        public Task Clean();
    }

    private sealed class Oracle : IDbContainer
    {
        private readonly IContainer _container;

        public Oracle(string edition = "free", string tag = "latest")
        {
            _container = new ContainerBuilder()
                .WithImage($"container-registry.oracle.com/database/{edition}:{tag}")
                .WithPortBinding(1521, true)
                .WithEnvironment("ORACLE_PWD", Password)
                .WithEnvironment("ORACLE_SID", "FREE")
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilPortIsAvailable(1521)
                    .UntilMessageIsLogged("DATABASE IS READY TO USE!")
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

                var connection = new OdbcConnection(
                    $"Driver={{Oracle in instantclient_21_12}};" +
                    $"Dbq=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT={_container.GetMappedPublicPort(1521)}))(CONNECT_DATA=(SERVICE_NAME=FREE)));" +
                    $"Uid=SYSTEM;Pwd={Password};"
                );

                connection.Open();

                return connection;
            }
        }

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();

        public async Task Clean()
        {
            if (DbConnection.State is not ConnectionState.Open)
            {
                throw new Exception("Connection must be open to clean database");
            }

            var respawner = await Respawner.CreateAsync(
                DbConnection,
                new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Oracle,
                    SchemasToInclude = new []{"SYSTEM"}
                }
            );

            Console.WriteLine(respawner.DeleteSql);

            await respawner.ResetAsync(DbConnection);
        }
    }

    private sealed class SqlServer : IDbContainer
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

                var connection = new OdbcConnection(
                    $"Driver={{ODBC Driver 17 for SQL Server}};" +
                    $"Server=127.0.0.1,{_container.GetMappedPublicPort(1433)};" +
                    $"UID=sa;PWD={Password};"
                );

                connection.Open();

                return connection;
            }
        }

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();

        public async Task Clean()
        {
            if (DbConnection.State is not ConnectionState.Open)
            {
                throw new Exception("Connection must be open to clean database");
            }

            var respawner = await Respawner.CreateAsync(
                DbConnection,
                new RespawnerOptions
                {
                    DbAdapter = DbAdapter.SqlServer
                }
            );

            await respawner.ResetAsync(DbConnection);
        }
    }

    private sealed class Postgres : IDbContainer
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

                var connection = new OdbcConnection(
                    $"Driver={{PostgreSQL UNICODE}};" +
                    $"Server=127.0.0.1;Port={_container.GetMappedPublicPort(5432)};" +
                    $"Uid=sa;Pwd={Password};"
                );

                connection.Open();

                return connection;
            }
        }

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();

        public async Task Clean()
        {
            if (DbConnection.State is not ConnectionState.Open)
            {
                throw new Exception("Connection must be open to clean database");
            }

            var respawner = await Respawner.CreateAsync(
                DbConnection,
                new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres
                }
            );

            await respawner.ResetAsync(DbConnection);
        }
    }

    private sealed class MySql : IDbContainer
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

                var connection = new OdbcConnection(
                    $"Driver={{MySQL ODBC 8.2 UNICODE Driver}};" +
                    $"Server=127.0.0.1;Port={_container.GetMappedPublicPort(3306)};" +
                    $"Database=test;" +
                    $"User=root;Password={Password};" +
                    $"MULTI_STATEMENTS=1;"
                );

                connection.Open();

                return connection;
            }
        }

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();

        public async Task Clean()
        {
            if (DbConnection.State is not ConnectionState.Open)
            {
                throw new Exception("Connection must be open to clean database");
            }

            var respawner = await Respawner.CreateAsync(
                DbConnection,
                new RespawnerOptions
                {
                    DbAdapter = DbAdapter.MySql
                }
            );

            await respawner.ResetAsync(DbConnection);
        }
    }
}