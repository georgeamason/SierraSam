using System.Text.Json;
using SierraSam.Core.Extensions;

namespace SierraSam.Core.Tests.Unit.Extensions;

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

    [TestCase("SELECT * FROM", ExpectedResult = "2cb62737ee53ada8dc1380e23a52496b")]
    [TestCase("INSERT INTO", ExpectedResult = "c455716319b4f5e68ab4fa7ed26068bc")]
    public string Checksum_returns_expected_hash(string contents)
    {
        return contents.Checksum();
    }
}