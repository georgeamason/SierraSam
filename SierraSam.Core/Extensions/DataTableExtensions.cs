using System.Data;

namespace SierraSam.Core.Extensions;

public static class DataTableExtensions
{
    /// <summary>
    /// Checks if the data table has the Flyway schema history table
    /// </summary>
    /// <param name="dataTable">The </param>
    /// <param name="configuration">The Flyway configuration</param>
    /// <returns></returns>
    public static bool HasMigrationHistory(this DataTable dataTable, Configuration configuration)
    {
        foreach (DataRow row in dataTable.Rows)
        {
            // https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/odbc-schema-collections
            var tableName = row["TABLE_NAME"].ToString();
            if (tableName == configuration.SchemaTable) return true;
        }

        return false;
    }
}