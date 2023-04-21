
using System.Data.Odbc;
using SierraSam.Core;

namespace SierraSam.Database;

public interface IDatabase
{
    string Name { get; }

    OdbcConnection Connection { get; }

    bool HasMigrationTable { get; }

    void CreateSchemaHistory(string schema, string table);

    IEnumerable<Migration> GetSchemaHistory(string schema, string table);

    void InsertIntoSchemaHistory(OdbcTransaction transaction, Migration migration);
}