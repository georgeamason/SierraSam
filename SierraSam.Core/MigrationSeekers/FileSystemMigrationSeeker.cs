using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace SierraSam.Core.MigrationSeekers;

internal sealed class FileSystemMigrationSeeker : IMigrationSeeker
{
    private readonly Configuration _configuration;

    private readonly IFileSystem _fileSystem;

    public FileSystemMigrationSeeker(Configuration configuration, IFileSystem fileSystem)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public IEnumerable<string> Find()
    {
        return _configuration.Locations
            .Where(d => d.StartsWith("filesystem:"))
            .SelectMany(d =>
            {
                var path = d.Split(':', 2).Last();

                return _fileSystem.Directory.GetFiles
                        (path, "*", SearchOption.AllDirectories)
                    .Where(migrationPath =>
                    {
                        var migration = new MigrationFile
                            (_fileSystem.FileInfo.New(migrationPath));

                        // V1__My_description.sql
                        // V1.1__My_description.sql
                        // V1.1.1.1.1.__My_description.sql
                        return Regex.IsMatch
                        ($"{migration.Filename}",
                            $"{_configuration.MigrationPrefix}\\d+(\\.?\\d{{0,}})+" +
                            $"{_configuration.MigrationSeparator}\\w+" +
                            $"({string.Join('|', _configuration.MigrationSuffixes)})");
                    });
            });
    }
}