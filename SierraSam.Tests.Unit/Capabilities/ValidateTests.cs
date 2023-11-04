using System.Collections;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Capabilities;
using SierraSam.Core.MigrationValidators;
using Spectre.Console;
using Spectre.Console.Testing;

namespace SierraSam.Tests.Unit.Capabilities;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class ValidateTests
{
    private readonly ILogger<Validate> _logger = Substitute.For<ILogger<Validate>>();
    private readonly IMigrationValidator _validator = Substitute.For<IMigrationValidator>();
    private readonly TestConsole _console = new();
    private readonly Validate _sut;

    public ValidateTests()
    {
        _sut = new Validate(_logger, _validator, _console);
    }

    private static IEnumerable Constructors()
    {
        // ReSharper disable ObjectCreationAsStatement
        yield return new TestCaseData(
            new TestDelegate(
                () => new Validate(
                    null!,
                    Substitute.For<IMigrationValidator>(),
                    Substitute.For<IAnsiConsole>()
                )
            )
        ).SetName("null logger");

        yield return new TestCaseData(
            new TestDelegate(
                () => new Validate(
                    Substitute.For<ILogger<Validate>>(),
                    null!,
                    Substitute.For<IAnsiConsole>()
                )
            )
        ).SetName("null migration validator");

        yield return new TestCaseData(
            new TestDelegate(
                () => new Validate(
                    Substitute.For<ILogger<Validate>>(),
                    Substitute.For<IMigrationValidator>(),
                    null!
                )
            )
        ).SetName("null console");
        // ReSharper restore ObjectCreationAsStatement
    }

    [TestCaseSource(nameof(Constructors))]
    public void Constructor_throws_for_null_args(TestDelegate constructor)
    {
        Assert.That(constructor, Throws.ArgumentNullException);
    }

    [Test]
    public void Validate_calls_migration_validator()
    {
        _sut.Run(Array.Empty<string>());

        _validator.Received(1).Validate();
    }

    [Test]
    public void Validate_writes_to_the_console_with_expected_message()
    {
        _validator.Validate().Returns(1);

        _sut.Run(Array.Empty<string>());

        _console.Output
            .NormalizeLineEndings()
            .Should()
            .Contain("Successfully validated 1 migration(s) (execution time");
    }
}