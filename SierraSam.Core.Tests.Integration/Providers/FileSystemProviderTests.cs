using SierraSam.Core.Providers;

namespace SierraSam.Core.Tests.Integration.Providers;

internal sealed class FileSystemProviderTests
{
    private readonly IFileSystemProvider _fileSystemProvider;
    
    public FileSystemProviderTests()
    {
        _fileSystemProvider = new FileSystemProvider();
    }

    [Test]
    public void File_exists_returns_correctly()
    {
        var path = Path.Combine(Environment.CurrentDirectory, "exampleFile.json");

        Assert.That(_fileSystemProvider.Exists(path), Is.True);
    }

    [Test]
    public void Read_all_text_reads_correctly()
    {
        var path = Path.Combine(Environment.CurrentDirectory, "exampleFile.json");

        Assert.That(_fileSystemProvider.ReadAllText(path), Is.EqualTo("{ \"key\":\"value\" }"));
    }
}