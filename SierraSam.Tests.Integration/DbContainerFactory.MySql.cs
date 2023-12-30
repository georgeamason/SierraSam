using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Respawn;

namespace SierraSam.Tests.Integration;

internal static partial class DbContainerFactory
{
    public sealed class MySql : ITestContainer
    {
        private readonly IContainer _container;

        private MySql(string tag = "latest")
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

        public static readonly IEnumerable<TestFixtureData> TestCases = new []
        {
            new TestFixtureData(new MySql("8.2"))
                .SetCategory("MySql")
                .SetArgDisplayNames("MySql-8.2"),

            new TestFixtureData(new MySql("5.7"))
                .SetCategory("MySql")
                .SetArgDisplayNames("MySql-5.7"),
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
                    DbAdapter = DbAdapter.MySql
                }
            );

            await respawner.ResetAsync(DbConnection);
        }
    }
}