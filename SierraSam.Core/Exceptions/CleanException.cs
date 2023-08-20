namespace SierraSam.Core.Exceptions;

public class CleanException : Exception
{
    public CleanException(string message) : base(message) { }

    public CleanException(string message, Exception innerException) : base(message, innerException) { }
}