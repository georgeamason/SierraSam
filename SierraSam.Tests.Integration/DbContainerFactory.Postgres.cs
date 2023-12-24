using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Respawn;

namespace SierraSam.Tests.Integration;

internal static partial class DbContainerFactory
{
    public sealed class Postgres : ITestContainer
    {
        private readonly IContainer _container;

        private Postgres(string tag = "latest")
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

        public static readonly IEnumerable<TestFixtureData> TestCases = new []
        {
            new TestFixtureData(new Postgres("16"))
                .SetCategory("Postgres")
                .SetArgDisplayNames("Postgres-16"),

            new TestFixtureData(new Postgres("15"))
                .SetCategory("Postgres")
                .SetArgDisplayNames("Postgres-15"),
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
                    $"Driver={{PostgreSQL UNICODE}};" +
                    $"Server=127.0.0.1;Port={_container.GetMappedPublicPort(5432)};" +
                    $"Uid=sa;Pwd={Password};"
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
                    DbAdapter = DbAdapter.Postgres
                }
            );

            await respawner.ResetAsync(DbConnection);
        }
    }
}