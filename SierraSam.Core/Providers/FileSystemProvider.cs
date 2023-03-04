﻿namespace SierraSam.Core.Providers;

public class FileSystemProvider : IFileSystemProvider
{
    public bool Exists(string path) => File.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);
}