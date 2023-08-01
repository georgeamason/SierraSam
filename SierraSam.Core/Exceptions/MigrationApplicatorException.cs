namespace SierraSam.Core.Exceptions;

internal sealed class MigrationApplicatorException : Exception
{
    public MigrationApplicatorException() : base("There was a problem applying migrations") { }

    public MigrationApplicatorException(string message) : base(message) { }

    public MigrationApplicatorException(string message, Exception innerException) : base(message, innerException) { }
}