using System.IO.Abstractions;

namespace SierraSam.Core.Tests.Integration.Providers;

internal sealed class FileSystemProviderTests
{
    private readonly IFileSystem _fileSystem;
    
    public FileSystemProviderTests()
    {
        _fileSystem = new FileSystem();
    }

    [Test]
    public void File_exists_returns_correctly()
    {
        var path = Path.Combine(Environment.CurrentDirectory, "exampleFile.json");

        Assert.That(_fileSystem.File.Exists(path), Is.True);
    }

    [Test]
    public void Read_all_text_reads_correctly()
    {
        var path = Path.Combine(Environment.CurrentDirectory, "exampleFile.json");

        Assert.That(_fileSystem.File.ReadAllText(path), Is.EqualTo("{ \"key\":\"value\" }"));
    }
}