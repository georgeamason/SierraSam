using System.Text.RegularExpressions;

namespace SierraSam.Core.Exceptions;

public class MigrationSeekerException : Exception
{
    public MigrationSeekerException() { }

    public MigrationSeekerException(string message) : base(message) { }

    public MigrationSeekerException(string message, Exception inner) : base(message, inner) { }
}