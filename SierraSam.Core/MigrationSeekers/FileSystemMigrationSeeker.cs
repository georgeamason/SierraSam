using System.IO.Abstractions;
using System.Text.RegularExpressions;
using SierraSam.Core.Constants;
using SierraSam.Core.Exceptions;

namespace SierraSam.Core.MigrationSeekers;

internal sealed class FileSystemMigrationSeeker : IMigrationSeeker
{
    private readonly IConfiguration _configuration;
    private readonly IFileSystem _fileSystem;

    public FileSystemMigrationSeeker(IConfiguration configuration, IFileSystem fileSystem)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public IReadOnlyCollection<PendingMigration> Find()
    {
        return _configuration.Locations
            .Where(location => location.StartsWith("filesystem:"))
            .Select(location => location[(location.IndexOf(':') + 1)..])
            .SelectMany(locationPath =>
            {
                try
                {
                    if (!_fileSystem.Directory.Exists(locationPath)) return Array.Empty<string>();

                    return _fileSystem.Directory
                        .GetFiles(locationPath, "*", SearchOption.AllDirectories)
                        .Where(filePath =>
                        {
                            var fileInfo = _fileSystem.FileInfo.New(filePath);

                            var migrationPrefixes = string.Join('|',
                                _configuration.MigrationPrefix,
                                _configuration.UndoMigrationPrefix,
                                _configuration.RepeatableMigrationPrefix);

                            var migrationSuffixes = string.Join('|',
                                _configuration.MigrationSuffixes
                                    .Select(suffix => @$"\{suffix}")
                                    .ToArray());

                            var pattern = @$"({migrationPrefixes}){MigrationRegex.VersionRegex}" +
                                          @$"{_configuration.MigrationSeparator}{MigrationRegex.DescriptionRegex}" +
                                          $"({migrationSuffixes})";

                            return Regex.IsMatch(
                                fileInfo.Name,
                                pattern,
                                RegexOptions.None,
                                new TimeSpan(0, 0, 2));
                        });
                }
                catch (UnauthorizedAccessException exception)
                {
                    throw new MigrationSeekerException
                    ($"The application does not have permission to access location '{locationPath}'",
                        exception);
                }
                catch (PathTooLongException exception)
                {
                    throw new MigrationSeekerException
                        ($"The location path '{locationPath}' is too long", exception);
                }
                catch (RegexMatchTimeoutException exception)
                {
                    throw new MigrationSeekerException
                        ("No match was found within the regular expression timeout", exception);
                }
            })
            .Select(locationPath => _fileSystem.FileInfo.New(locationPath))
            .Select(fileInfo => PendingMigration.Parse(_configuration, fileInfo))
            .ToArray();
    }


}