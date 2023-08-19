using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.Exceptions;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core.Tests.Integration.MigrationSeekers;

public class FileSystemMigrationSeekerTests
{
    [TestCase("V1__My_description.sql", "1", "My_description")]
    [TestCase("R__My_description.sql", null, "My_description", MigrationType.Repeatable)]
    [TestCase("V2__Desc.sql", "2", "Desc")]
    [TestCase("V2__Add a new table.sql", "2", "Add a new table")]
    [TestCase("V1004__make_v11_sql_monitor_license.sql", "1004", "make_v11_sql_monitor_license")]
    [TestCase("V1003__delete invalid license.sql", "1003", "delete invalid license")]
    [TestCase("V2023.01.12.4343__create_users_table.sql", "2023.01.12.4343", "create_users_table")]
    public void Find_returns_expected_migrations(
        string fileName,
        string? version,
        string description,
        MigrationType type = MigrationType.Versioned)
    {
        var searchPath = Path.Combine("db", "migrations");

        var configuration = new Configuration
            (locations: new []{ $"filesystem:{searchPath}" },
             migrationPrefix: "V",
             migrationSeparator: "__",
             migrationSuffixes: new []{ ".sql" });

        var fileSystem = new MockFileSystem();

        fileSystem.Directory.CreateDirectory(searchPath);

        fileSystem.File.Create
            (Path.Combine(searchPath, fileName));

        // Add bad file
        const string badFileName = "V1__My_description.txt";
        fileSystem.File.Create(Path.Combine(searchPath, badFileName));

        var migrationSeeker = new FileSystemMigrationSeeker
            (configuration, fileSystem);

        var migrations = migrationSeeker.Find();

        migrations
            .Should()
            .BeEquivalentTo(new []
            {
                new PendingMigration(
                    version,
                    description,
                    type,
                    string.Empty.Checksum(),
                    Path.Combine("C:", searchPath, fileName),
                    fileName)
            });
    }

    [Test]
    public void Find_searches_directory_recursively()
    {
        var searchPath = Path.Combine("db", "migrations");

        var configuration = new Configuration
            (locations: new []{ $"filesystem:{searchPath}" },
             migrationPrefix: "V",
             migrationSeparator: "__",
             migrationSuffixes: new []{ ".sql" });

        var fileSystem = new MockFileSystem();

        fileSystem.Directory.CreateDirectory(searchPath);

        // Add sub directory to search path
        var subDirectory = Path.Combine(searchPath, "subdir");
        fileSystem.Directory.CreateDirectory(subDirectory);

        fileSystem.File.Create
            (Path.Combine(subDirectory, "V1__My_description.sql"));

        var migrationSeeker = new FileSystemMigrationSeeker
            (configuration, fileSystem);

        var migrations = migrationSeeker.Find();

        migrations
            .Should()
            .BeEquivalentTo(new[]
            {
                new PendingMigration(
                    "1",
                    "My_description",
                    MigrationType.Versioned,
                    string.Empty.Checksum(),
                    Path.Combine("C:", searchPath, @"subdir\V1__My_description.sql"),
                    "V1__My_description.sql")
            });
    }

    [TestCase("filesystem:")]
    [TestCase("filesystem")]
    [TestCase(":")]
    public void Find_returns_empty_collection_for_bad_location(string location)
    {
        var configuration = new Configuration
            (locations: new []{ location });

        var fileSystem = Substitute.For<IFileSystem>();

        var migrationSeeker = new FileSystemMigrationSeeker
            (configuration, fileSystem);

        var migrations = migrationSeeker.Find();

        migrations
            .Should()
            .BeEquivalentTo(Enumerable.Empty<PendingMigration>());
    }

    [Test]
    public void Find_throws_MigrationSeekerException_for_unauthorized_access()
    {
        var searchPath = Path.Combine("db", "migrations");

        var configuration = new Configuration
            (locations: new []{ $"filesystem:{searchPath}" });

        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem.Directory
            .When(d => d.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()))
            .Do(_ => throw new UnauthorizedAccessException());

        var migrationSeeker = new FileSystemMigrationSeeker
            (configuration, fileSystem);

        migrationSeeker
            .Invoking(s => s.Find())
            .Should()
            .Throw<MigrationSeekerException>()
            .WithMessage($"The application does not have permission to access location '{searchPath}'");
    }

    [Test]
    public void Find_throws_MigrationSeekerException_for_missing_directory()
    {
        var searchPath = Path.Combine("db", "migrations");

        var configuration = new Configuration
            (locations: new []{ $"filesystem:{searchPath}" });

        var fileSystem = new MockFileSystem();

        var migrationSeeker = new FileSystemMigrationSeeker
            (configuration, fileSystem);

        migrationSeeker
            .Invoking(s => s.Find())
            .Should()
            .Throw<MigrationSeekerException>()
            .WithMessage($"The directory '{searchPath}' does not exist");
    }

    [Test]
    public void Find_throws_MigrationSeekerException_for_path_too_long()
    {
        const string searchPath = "";

        var configuration = new Configuration
            (locations: new []{ $"filesystem:{searchPath}" });

        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem.Directory
            .When(d => d.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()))
            .Do(_ => throw new PathTooLongException());

        var migrationSeeker = new FileSystemMigrationSeeker
            (configuration, fileSystem);

        migrationSeeker
            .Invoking(s => s.Find())
            .Should()
            .Throw<MigrationSeekerException>()
            .WithMessage($"The location path '{searchPath}' is too long");
    }
}