using System.Data.Odbc;

namespace SierraSam.Core;

public interface IDatabase
{
    string Name { get; }

    OdbcConnection Connection { get; }

    bool HasMigrationTable { get; }

    bool HasTable(string tableName);

    void CreateSchemaHistory(string? schema = null, string? table = null);

    /// <summary>
    /// Get the schema history for the given schema and table.
    /// If no schema and table is provided, the defaults are used.
    /// </summary>
    /// <param name="schema">The database schema</param>
    /// <param name="table">The database table</param>
    /// <returns>A collection of applied migrations</returns>
    IReadOnlyCollection<AppliedMigration> GetSchemaHistory(string? schema = null, string? table = null);

    void InsertSchemaHistory(AppliedMigration appliedMigration, OdbcTransaction? transaction = null);

    void UpdateSchemaHistory(AppliedMigration appliedMigration, OdbcTransaction? transaction = null);

    TimeSpan ExecuteMigration(string sql, OdbcTransaction? transaction = null);

    IReadOnlyCollection<DatabaseObject> GetSchemaObjects(string? schema = null, OdbcTransaction? transaction = null);

    void DropSchemaObject(DatabaseObject obj, OdbcTransaction? transaction = null);
}