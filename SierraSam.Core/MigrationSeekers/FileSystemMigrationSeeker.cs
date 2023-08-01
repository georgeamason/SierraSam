using System.IO.Abstractions;
using System.Text.RegularExpressions;
using SierraSam.Core.Exceptions;

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

    public IReadOnlyCollection<string> Find()
    {
        return _configuration.Locations
            .Where(location => location.StartsWith("filesystem:"))
            .Select(location => location[(location.IndexOf(':') + 1)..])
            .SelectMany(locationPath =>
            {
                try
                {
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

                            var pattern = @$"({migrationPrefixes})((\d+)((\.{{1}}\d+)*)(\.{{0}}))?" +
                                          @$"{_configuration.MigrationSeparator}(\w|\s)+" +
                                          $"({migrationSuffixes})";

                            return Regex.IsMatch
                                (fileInfo.Name,
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
                catch (DirectoryNotFoundException exception)
                {
                    throw new MigrationSeekerException
                        ($"The directory '{locationPath}' does not exist", exception);
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
            .ToArray();
    }


}