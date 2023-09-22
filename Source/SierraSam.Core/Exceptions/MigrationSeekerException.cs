using System.Text.RegularExpressions;

namespace SierraSam.Core.Exceptions;

public class MigrationSeekerException : Exception
{
    public MigrationSeekerException() : base("There was a problem finding migrations") { }

    public MigrationSeekerException(string message) : base(message) { }

    public MigrationSeekerException(string message, Exception inner) : base(message, inner) { }
}