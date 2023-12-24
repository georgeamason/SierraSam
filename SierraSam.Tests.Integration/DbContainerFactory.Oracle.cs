using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Respawn;

namespace SierraSam.Tests.Integration;

internal static partial class DbContainerFactory
{
    public sealed class Oracle : ITestContainer
    {
        private readonly IContainer _container;

        private Oracle(string edition = "free", string tag = "latest")
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

        public static readonly IEnumerable<TestFixtureData> TestCases = new []
        {
            new TestFixtureData(new Oracle("free", "23.3.0.0"))
                .SetCategory("Oracle")
                .SetArgDisplayNames("Oracle-23.3.0.0"),

            // new Oracle("express", "21.3.0-xe"),
            // new Oracle("express", "18.4.0-xe"),
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
                    $"Driver={{Oracle 21 ODBC driver}};" +
                    $"Dbq=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT={_container.GetMappedPublicPort(1521)}))(CONNECT_DATA=(SERVICE_NAME=FREE)));" +
                    $"Uid=SYSTEM;Pwd={Password};"
                );

                connection.Open();

                return connection;
            }
        }

        public void AddTable() => throw new NotImplementedException();

        public void AddFunction() => throw new NotImplementedException();

        public void AddProcedure() => throw new NotImplementedException();

        public void AddView(string schema) => throw new NotImplementedException();

        public void AddType() => throw new NotImplementedException();

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
                    DbAdapter = DbAdapter.Oracle,
                    SchemasToInclude = new []{"SYSTEM"}
                }
            );

            Console.WriteLine(respawner.DeleteSql);

            await respawner.ResetAsync(DbConnection);
        }
    }
}