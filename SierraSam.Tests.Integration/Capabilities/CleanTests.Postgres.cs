using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SierraSam.Capabilities;
using SierraSam.Core;
using SierraSam.Database;
using Spectre.Console.Testing;
using static SierraSam.Tests.Integration.DbContainerFactory;

namespace SierraSam.Tests.Integration.Capabilities;

[TestFixtureSource(typeof(Postgres), nameof(Postgres.TestCases))]
internal sealed class PostgresCleanTests
{
    private readonly ITestContainer _testContainer;
    private readonly ILogger<Clean> _logger = Substitute.For<ILogger<Clean>>();
    private readonly TestConsole _console = new ();

    public PostgresCleanTests(ITestContainer testContainer) => _testContainer = testContainer;

    [SetUp]
    public Task SetUp()
    {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (_testContainer.State is not TestcontainersStates.Running)
        {
            return _testContainer.StartAsync();
        }

        return Task.CompletedTask;
    }

    [Test]
    public void Clean_drops_all_schema_objects()
    {
        var connection = _testContainer.DbConnection;

        var configuration = new Configuration(url: connection.ConnectionString);

        var database = DatabaseResolver.Create(
            new NullLoggerFactory(),
            connection,
            new DbExecutor(connection),
            configuration,
            new MemoryCache(new MemoryCacheOptions())
        );

        var sut = new Clean(_logger, database, configuration, _console);

        database.ExecuteMigration("CREATE DOMAIN name_component VARCHAR(255) NOT NULL;");

        database.ExecuteMigration(
            """
            CREATE TABLE employee (
                EmployeeID INT PRIMARY KEY,
                Forename name_component,
                Surname name_component,
                ManagerID INT,
                FOREIGN KEY (ManagerID) REFERENCES employee(EmployeeID)
            );
            """
        );

        database.ExecuteMigration(
            """
            CREATE FUNCTION fn_employee_name(id INT)
            RETURNS VARCHAR(255)
            LANGUAGE plpgsql
            AS
            $$
            BEGIN
            RETURN (SELECT concat_ws(' ', Forename, Surname) FROM employee WHERE employee.EmployeeID = id);
            END
            $$
            """
        );

        database.ExecuteMigration(
            $"""
             CREATE VIEW managers
             AS
             SELECT
             EmployeeID,
             {database.DefaultSchema}.fn_employee_name(EmployeeID) as Name
             FROM employee
             WHERE ManagerID IS NOT NULL
             """
        );

        database.ExecuteMigration(
            """
            CREATE PROCEDURE sp_add_employee(forename varchar, surname varchar)
            LANGUAGE plpgsql
            AS
            $$
            DECLARE id int = (SELECT COALESCE(MAX(EmployeeID), 0) + 1 FROM employee);
            BEGIN
            INSERT INTO employee (EmployeeID, Forename, Surname, ManagerID)
            VALUES (id, forename, surname, null);
            END;
            $$
            """
        );

        sut.Run(Array.Empty<string>());

        database.HasTable(table: "employee").Should().BeFalse();
        database.HasView(view: "managers").Should().BeFalse();
        database.HasRoutine(routine: "fn_employee_name").Should().BeFalse();
        database.HasRoutine(routine: "sp_add_employee").Should().BeFalse();
        database.HasDomain(domain: "name_component").Should().BeFalse();
    }
}