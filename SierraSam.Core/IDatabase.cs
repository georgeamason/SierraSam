using System.Data;

namespace SierraSam.Core;

public interface IDatabase
{
    string Provider { get; }

    string ServerVersion { get; }

    string? DefaultSchema { get; }

    IDbConnection Connection { get; }

    bool HasMigrationTable { get; }

    bool HasTable(string tableName);

    void CreateSchemaHistory(string? schema = null, string? table = null, IDbTransaction? transaction = null);

    /// <summary>
    /// Get the schema history for the given schema and table.
    /// If no schema and table is provided, the defaults are used.
    /// </summary>
    /// <param name="schema">The database schema</param>
    /// <param name="table">The database table</param>
    /// <returns>A collection of applied migrations</returns>
    IReadOnlyCollection<AppliedMigration> GetSchemaHistory(string? schema = null, string? table = null);

    int InsertSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null);

    int UpdateSchemaHistory(AppliedMigration appliedMigration, IDbTransaction? transaction = null);

    TimeSpan ExecuteMigration(string sql, IDbTransaction? transaction = null);

    IReadOnlyCollection<DatabaseObject> GetSchemaObjects(string? schema = null, IDbTransaction? transaction = null);

    void DropSchemaObject(DatabaseObject obj, IDbTransaction? transaction = null);

    int GetInstalledRank(string? schema = null, string? table = null, IDbTransaction? transaction = null);
}