namespace SierraSam.Core.Exceptions;

public class OdbcExecutorException : Exception
{
    public OdbcExecutorException(): base("Failed to execute the SQL query") { }

    public OdbcExecutorException(string message) : base(message) { }

    public OdbcExecutorException(string message, Exception inner) : base(message, inner) { }
}