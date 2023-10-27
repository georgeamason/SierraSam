using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Enums;
using SierraSam.Core.Exceptions;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Core.Tests.Unit.MigrationSeekers;

internal sealed class FileSystemMigrationSeekerTests
{
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    private static IEnumerable Initialisation_with_null_args()
    {
        yield return new TestCaseData
            (new TestDelegate
                (() => new FileSystemMigrationSeeker(null!, Substitute.For<IFileSystem>())))
            .SetName("Null configuration");

        yield return new TestCaseData
            (new TestDelegate
                (() => new FileSystemMigrationSeeker(Substitute.For<IConfiguration>(), null!)))
            .SetName("Null file system");
    }

    [TestCaseSource(nameof(Initialisation_with_null_args))]
    public void Constructor_throws_for_null_args(TestDelegate constructor)
    {
        constructor
            .Invoking(c => c.Invoke())
            .Should()
            .Throw<ArgumentNullException>();
    }

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

        var configuration = Substitute.For<IConfiguration>();

        configuration.Locations.Returns(new []{ $"filesystem:{searchPath}" });
        configuration.RepeatableMigrationPrefix.Returns("R");
        configuration.MigrationPrefix.Returns("V");
        configuration.UndoMigrationPrefix.Returns("U");
        configuration.MigrationSeparator.Returns("__");
        configuration.MigrationSuffixes.Returns(new []{ ".sql" });

        var fileSystem = new MockFileSystem();

        fileSystem.Directory.CreateDirectory(searchPath);

        fileSystem.File.Create(Path.Combine(searchPath, fileName));

        // Add bad file
        const string badFileName = "V1__My_description.txt";
        fileSystem.File.Create(Path.Combine(searchPath, badFileName));

        var migrationSeeker = new FileSystemMigrationSeeker(
            configuration,
            fileSystem);

        var migrations = migrationSeeker.Find();

        migrations
            .Should()
            .BeEquivalentTo(new []
            {
                new PendingMigration(
                    version,
                    description,
                    type,
                    string.Empty,
                    fileName)
            });
    }

    [Test]
    public void Find_searches_directory_recursively()
    {
        var searchPath = Path.Combine("db", "migrations");

        var configuration = Substitute.For<IConfiguration>();

        configuration.Locations.Returns(new []{ $"filesystem:{searchPath}" });
        configuration.RepeatableMigrationPrefix.Returns("R");
        configuration.MigrationPrefix.Returns("V");
        configuration.MigrationSeparator.Returns("__");
        configuration.MigrationSuffixes.Returns(new []{ ".sql" });

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
                    string.Empty,
                    "V1__My_description.sql")
            });
    }

    [TestCase("filesystem:")]
    [TestCase("filesystem")]
    [TestCase(":")]
    public void Find_returns_empty_collection_for_bad_location(string location)
    {
        var configuration = Substitute.For<IConfiguration>();

        configuration.Locations.Returns(new []{ location });

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

        var configuration = Substitute.For<IConfiguration>();

        configuration.Locations.Returns(new []{ $"filesystem:{searchPath}" });

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Exists(searchPath).Returns(true);

        fileSystem.Directory
            .When(d => d.GetFiles(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<SearchOption>())
            )
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
    public void Find_throws_MigrationSeekerException_for_path_too_long()
    {
        const string searchPath = "";

        var configuration = Substitute.For<IConfiguration>();
        configuration.Locations.Returns(new []{ $"filesystem:{searchPath}" });

        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem.Directory.Exists(searchPath).Returns(true);

        fileSystem.Directory
            .When(d => d.GetFiles(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<SearchOption>())
            )
            .Do(_ => throw new PathTooLongException());

        var migrationSeeker = new FileSystemMigrationSeeker(configuration, fileSystem);

        migrationSeeker
            .Invoking(s => s.Find())
            .Should()
            .Throw<MigrationSeekerException>()
            .WithMessage($"The location path '{searchPath}' is too long");
    }

    [Test]
    public void Find_returns_empty_array_when_location_does_not_exist()
    {
        var searchPath = Path.Combine("db", "migrations");

        var configuration = Substitute.For<IConfiguration>();
        configuration.Locations.Returns(new []{ $"filesystem:{searchPath}" });

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Exists(searchPath).Returns(false);

        var sut = new FileSystemMigrationSeeker(configuration, fileSystem);
        var migrations = sut.Find();

        fileSystem.Directory.Received().Exists(searchPath);

        migrations
            .Should()
            .BeEquivalentTo(Array.Empty<PendingMigration>());
    }
}