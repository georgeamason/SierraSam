namespace SierraSam.Core.Tests.Unit;

internal sealed class OdbcExecutorTests
{
    [Test]
    public void Cannot_construct_with_null_connection()
    {
        Assert.That(() => new OdbcExecutor(null!), Throws.TypeOf<ArgumentNullException>());
    }
}