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

[TestFixtureSource(typeof(SqlServer), nameof(SqlServer.TestCases))]
internal sealed class SqlServerCleanTests
{
    private readonly ITestContainer _testContainer;
    private readonly ILogger<Clean> _logger = Substitute.For<ILogger<Clean>>();
    private readonly TestConsole _console = new ();

    public SqlServerCleanTests(ITestContainer testContainer) => _testContainer = testContainer;

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

        database.ExecuteMigration("CREATE TYPE NAME_COMPONENT FROM VARCHAR(255) NOT NULL;");

        database.ExecuteMigration(
            """
            CREATE TABLE Employee (
                EmployeeID INT PRIMARY KEY,
                Forename NAME_COMPONENT,
                Surname NAME_COMPONENT,
                ManagerID INT,
                FOREIGN KEY (ManagerID) REFERENCES Employee(EmployeeID)
            );
            """
        );

        database.ExecuteMigration(
            """
            CREATE FUNCTION fn_employee_name(@employeeId INT)
            RETURNS VARCHAR(255)
            AS
            BEGIN
            RETURN (SELECT CONCAT_WS(' ', Forename, Surname) FROM Employee WHERE EmployeeID = @employeeId);
            END
            """
        );

        database.ExecuteMigration(
            $"""
             CREATE VIEW Managers
             AS
             SELECT
             EmployeeID,
             {database.DefaultSchema}.fn_employee_name(EmployeeID) as Name
             FROM Employee
             WHERE ManagerID IS NOT NULL
             """
        );

        database.ExecuteMigration(
            """
            CREATE PROCEDURE sp_add_employee
                @forename NVARCHAR(MAX),
                @surname NVARCHAR(MAX)
            AS
            BEGIN
                DECLARE @id INT = (SELECT COALESCE(MAX(EmployeeID), 0) + 1 FROM Employee);
            
                INSERT INTO Employee (EmployeeID, Forename, Surname, ManagerID)
                VALUES (@id, @forename, @surname, NULL);
            END;
            """
        );

        sut.Run(Array.Empty<string>());

        database.HasTable(table: "Employee").Should().BeFalse();
        database.HasView(view: "Managers").Should().BeFalse();
        database.HasRoutine(routine: "fn_employee_name").Should().BeFalse();
        database.HasRoutine(routine: "sp_add_employee").Should().BeFalse();
        database.HasDomain(domain: "NAME_COMPONENT").Should().BeFalse();
    }
}