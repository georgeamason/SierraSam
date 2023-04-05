using System.IO.Abstractions.TestingHelpers;

namespace SierraSam.Core.Tests.Unit;

[TestFixture]
internal sealed class MigrationTests
{
    [Test]
    public void Constructor_null_argument_throws()
    {
        Assert.That(
            () => new Migration(null),
            Throws.TypeOf<ArgumentNullException>());
    }

    [TestCase("./RRR2.2.2223__Add_new_table.sql", "RRR", "2.2.2223", "__", "Add_new_table")]
    [TestCase("./V2__Desc.sql", "V", "2", "__", "Desc")]
    [TestCase("./VV22.12.2@@xyz.abc", "VV", "22.12.2", "@@", "xyz")]
    [TestCase("./U1.1__Fix_indexes.sql", "U", "1.1", "__", "Fix_indexes")]
    [TestCase("./V2__Add a new table.sql", "V", "2", "__", "Add a new table")]
    public void Properties_return_expected_result
        (string filePath, string prefix, string version, string separator, string description)
    {
        var mockFileSystem = new MockFileSystem();

        mockFileSystem.AddFile
            (filePath, new MockFileData("fake-content"));

        var mockFileInfo = new MockFileInfo(mockFileSystem, filePath);

        var migration = new Migration(mockFileInfo);

        Assert.That(migration.Prefix, Is.EqualTo(prefix));
        Assert.That(migration.Version, Is.EqualTo(version));
        Assert.That(migration.Separator, Is.EqualTo(separator));
        Assert.That(migration.Description, Is.EqualTo(description));
    }
}