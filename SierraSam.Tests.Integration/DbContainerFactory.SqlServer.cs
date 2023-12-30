using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Respawn;

namespace SierraSam.Tests.Integration;

internal static partial class DbContainerFactory
{
    public sealed class SqlServer : ITestContainer
    {
        private readonly IContainer _container;

        private SqlServer(string tag = "latest")
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

        public static readonly IEnumerable<TestFixtureData> TestCases = new []
        {
            new TestFixtureData(new SqlServer("2022-latest"))
                .SetCategory("SqlServer")
                .SetArgDisplayNames("SqlServer-2022"),

            new TestFixtureData(new SqlServer("2017-latest"))
                .SetCategory("SqlServer")
                .SetArgDisplayNames("SqlServer-2017"),
        };

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

        public TestcontainersStates State => _container.State;

        public Task StartAsync() => _container.StartAsync();

        public Task StopAsync() => _container.StopAsync();

        public async Task Reset()
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
}