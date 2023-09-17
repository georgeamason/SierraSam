namespace SierraSam.Core.Exceptions;

public sealed class MigrationValidatorException : Exception
{
    public MigrationValidatorException() : base("There was a problem validating migration(s)") { }

    public MigrationValidatorException(string message) : base(message) { }

    public MigrationValidatorException(string message, Exception innerException) : base(message, innerException) { }
}