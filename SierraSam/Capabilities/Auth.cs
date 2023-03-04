using Microsoft.Extensions.Logging;
using RedGate.Client.Activation.Shim;

namespace SierraSam.Capabilities;

public sealed class Auth : ICapability
{
    public Auth(ILogger<Auth> logger, ILicenseClient licenseClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _licenseClient = licenseClient ?? throw new ArgumentNullException(nameof(licenseClient));
    }

    public void Run(string[] args)
    {
        _logger.LogInformation($"{nameof(Auth)} is running");

        _licenseClient.GetLicense();
    }

    private readonly ILogger<Auth> _logger;

    private readonly ILicenseClient _licenseClient;
}