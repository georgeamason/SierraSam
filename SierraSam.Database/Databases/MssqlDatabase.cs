using System.Data;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public class MssqlDatabase : DefaultDatabase
{
    private readonly IConfiguration _configuration;
    private readonly DbExecutor _dbExecutor;

    public MssqlDatabase(IDbConnection connection, IConfiguration configuration)
        : base(connection, configuration)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _dbExecutor = new DbExecutor(connection);
    }

    public override string Provider => "MSSQL";

    public override string ServerVersion =>
        _dbExecutor.ExecuteScalar<string>("SELECT SERVERPROPERTY('productversion')")!;

    public override string DefaultSchema =>
        _dbExecutor.ExecuteScalar<string>("SELECT SCHEMA_NAME()")!;
}