using Microsoft.Extensions.Logging;
using RedGate.Client.Activation.Shim;

namespace SierraSam.Licensing;

internal static class LicenseClientFactory
{
    public static ILicenseClient Create(ILogger logger)
    {
        return new LicenseClient(logger);
    }
}