﻿using System.Data.Odbc;

namespace SierraSam.Core;

public interface IDatabase
{
    string Name { get; }

    OdbcConnection Connection { get; }

    bool HasMigrationTable { get; }

    bool HasTable(string tableName);

    void CreateSchemaHistory(string schema, string table);

    IReadOnlyCollection<AppliedMigration> GetSchemaHistory(string schema, string table);

    void InsertSchemaHistory(OdbcTransaction transaction, AppliedMigration appliedMigration);

    TimeSpan ExecuteMigration(OdbcTransaction transaction, string sql);
}