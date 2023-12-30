using NUnit.Framework.Internal;

namespace SierraSam.Tests.Integration;

internal static class TestExtensions
{
    public static TestFixtureData SetCategory(this TestFixtureData fixtureData, string category)
    {
        fixtureData.Properties.Set(PropertyNames.Category, category);
        return fixtureData;
    }
}