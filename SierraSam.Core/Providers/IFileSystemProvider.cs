namespace SierraSam.Core.Providers;

public interface IFileSystemProvider
{
    bool Exists(string path);

    string ReadAllText(string path);
}