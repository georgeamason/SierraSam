namespace SierraSam.Exceptions;

public sealed class LicenseException : Exception
{
    public LicenseException(string? message)
        : base(message) { }
}