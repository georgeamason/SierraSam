namespace SierraSam.Core.Tests.Unit;

internal sealed class VersionComparatorTests
{
    [TestCase("1.2.3", "1.2.2", ExpectedResult = true)]
    [TestCase("1_2_3", "1_2_2", ExpectedResult = true)]
    [TestCase("123.456.6", "123.456.7", ExpectedResult = false)]
    [TestCase("2022.07.26.22.41", "2013.01.15.11.35", ExpectedResult = true)]
    [TestCase("2", "1", ExpectedResult = true)]
    public bool Comparing_versions_returns_correct_result
        (string version1, string version2)
    {
        return VersionComparator.Compare(version1, version2);
    }
}