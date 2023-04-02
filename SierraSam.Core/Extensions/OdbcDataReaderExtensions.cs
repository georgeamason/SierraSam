using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Text;

namespace SierraSam.Core.Extensions;

public static class OdbcDataReaderExtensions
{
    public static DataTable? GetData(this OdbcDataReader dataReader)
    {
        if (!dataReader.HasRows)
            return null;

        var dataTable = new DataTable();

        var canGetColumnSchema = dataReader.CanGetColumnSchema();
        if (canGetColumnSchema)
        {
            var columns = dataReader.GetColumnSchema();
            foreach (var col in columns)
            {
                dataTable.Columns.Add(col.ColumnName, col.DataType!);
            }
        }

        while (dataReader.Read())
        {
            var dataRow = dataTable.NewRow();
            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                if (!canGetColumnSchema)
                {
                    var columnName = dataReader.GetName(i);
                    dataTable.Columns.Add(columnName);
                }

                dataRow[i] = dataReader[i];
            }

            dataTable.Rows.Add(dataRow);
        }

        return dataTable;
    }
}