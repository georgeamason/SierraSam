using System.Data;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public class MssqlDatabase : DefaultDatabase
{
    private readonly IConfiguration _configuration;
    private readonly OdbcExecutor _odbcExecutor;

    public MssqlDatabase(IDbConnection connection, IConfiguration configuration)
        : base(connection, configuration)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _odbcExecutor = new OdbcExecutor(connection);
    }

    public override string Provider => "MSSQL";

    public override string ServerVersion =>
        _odbcExecutor.ExecuteScalar<string>("SELECT SERVERPROPERTY('productversion')")!;
}