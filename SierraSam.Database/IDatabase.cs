
using System.Data.Odbc;
using SierraSam.Core;

namespace SierraSam.Database;

public interface IDatabase
{
    string Name { get; }

    OdbcConnection Connection { get; }

    bool HasMigrationTable { get; }

    bool HasTable(string tableName);

    void CreateSchemaHistory(string schema, string table);

    IEnumerable<Migration> GetSchemaHistory(string schema, string table);

    void InsertSchemaHistory(OdbcTransaction transaction, Migration migration);

    TimeSpan ExecuteMigration(OdbcTransaction transaction, string sql);
}