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

[TestFixtureSource(typeof(Oracle), nameof(Oracle.TestCases))]
internal sealed class OracleCleanTests
{
    private readonly ITestContainer _testContainer;
    private readonly ILogger<Clean> _logger = Substitute.For<ILogger<Clean>>();
    private readonly TestConsole _console = new ();

    public OracleCleanTests(ITestContainer testContainer) => _testContainer = testContainer;

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

        database.ExecuteMigration("CREATE TYPE NAME_COMPONENT IS VARRAY(1) OF VARCHAR(255) NOT NULL;");

        database.ExecuteMigration(
            """
            CREATE TABLE Employee (
                EmployeeID int PRIMARY KEY NOT NULL,
                Forename NAME_COMPONENT,
                Surname NAME_COMPONENT,
                ManagerID int NULL,
                FOREIGN KEY (ManagerID) REFERENCES Employee (EmployeeID)
            );
            """
        );

        database.ExecuteMigration(
            """
            CREATE OR REPLACE FUNCTION fn_employee_name(p_id IN INT)
            RETURN VARCHAR2
            AS
              v_full_name VARCHAR2(255);
            BEGIN
              SELECT Forename || ' ' || Surname
              INTO v_full_name
              FROM Employee
              WHERE EmployeeID = p_id;
            
              RETURN v_full_name;
            END;
            """
        );

        database.ExecuteMigration(
            $"""
             CREATE VIEW Managers
             AS
             SELECT
             EmployeeID,
             C##RODEO.FN_EMPLOYEE_NAME(EmployeeID) as Name
             FROM Employee
             WHERE ManagerID IS NOT NULL;
             """
        );

        database.ExecuteMigration(
            """
            CREATE OR REPLACE PROCEDURE sp_add_employee (
                p_forename VARCHAR2,
                p_surname VARCHAR2
            )
            AS
                v_id NUMBER;
            BEGIN
                -- Find the maximum EmployeeID and increment it by 1
                SELECT NVL(MAX(EmployeeID), 0) + 1 INTO v_id FROM Employee;
            
                -- Insert the new employee
                INSERT INTO Employee (EmployeeID, Forename, Surname, ManagerID)
                VALUES (v_id, p_forename, p_surname, NULL);
            
                COMMIT; -- Commit the transaction (if necessary)
            END sp_add_employee;
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