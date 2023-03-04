using System.Text.Json;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using SierraSam.Core.Extensions;

namespace SierraSam.Tests.Unit.Extensions;

[TestFixture]
internal sealed class StringExtensionsTests
{
    [TestCase("{ \"key\": \"value\" }", true)]
    [TestCase("{ \"key\": \"valueWithTrailingComma\", }", true)]
    [TestCase("{ //There is a comment here \n \"key\": \"value\" }", true)]
    [TestCase("{ fakeJson }", false)]
    public void IsJson_handles_correctly(string json, bool isValid)
    {
        Assert.That(() => json.IsJson(out var ex), Is.EqualTo(isValid));
    }

    [TestCase("{ fakeJson }")]
    [TestCase("{ \"key\": valueIsBad }")]
    [TestCase("{ \"key\": 0,,, }")]
    public void IsJson_outputs_correctly(string json)
    {
        json.IsJson(out var ex);

        Assert.That(ex, Is.InstanceOf<JsonException>());
    }
}