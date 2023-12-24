using System.Data.Common;
using DotNet.Testcontainers.Containers;

namespace SierraSam.Tests.Integration;

internal static partial class DbContainerFactory
{
    private const string Password = "yourStrong(!)Password";

    public interface ITestContainer
    {
        public DbConnection DbConnection { get; }
        public TestcontainersStates State { get; }
        public Task StartAsync();
        public Task StopAsync();
        public Task Reset();
    }
}