using Source191.Vantage.Adapters.Processes;
using System.Text;
using System.Text.RegularExpressions;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class OpenFileScriptShould
{
    [Theory]
    [InlineData(@"C:\Users\me\source\repos\Northwind & Co's tools\Northwind.Orders.slnx")]
    [InlineData(@"C:\Users\me\100% ^ (a)\[1]\é☃.sln")]
    [InlineData("https://example.test/pulls/42?a=1&b=%25x&c='d'")]
    public void HoldNoPartOfTheTargetInClear(string target)
    {
        string script = OpenFileScript.For(target);

        script.ShouldNotContain("Northwind");
        script.ShouldNotContain("example.test");
        script.ShouldNotContain("&");
        script.ShouldNotContain("%");
        script.Count(character => character == '\'').ShouldBe(2, "the only quotes are around the encoded target");
    }

    [Theory]
    [InlineData(@"C:\Users\me\source\repos\Northwind & Co's tools\Northwind.Orders.slnx")]
    [InlineData(@"C:\Users\me\100% ^ (a)\[1]\é☃.sln")]
    [InlineData("https://example.test/pulls/42?a=1&b=%25x&c='d'")]
    public void DecodeToTheSameTarget(string target)
    {
        string script = OpenFileScript.For(target);

        Match encoded = Regex.Match(script, @"FromBase64String\('(?<text>[A-Za-z0-9+/=]*)'\)");
        encoded.Success.ShouldBeTrue("the target travels as Base64 in a single-quoted literal");
        Encoding.UTF8.GetString(Convert.FromBase64String(encoded.Groups["text"].Value)).ShouldBe(target);
    }

    [Fact]
    public void ReportAFailureOnStandardErrorAndExitOne()
    {
        string script = OpenFileScript.For(@"C:\Users\me\Northwind.Orders.slnx");

        script.ShouldContain("[Console]::Error.WriteLine($_.Exception.Message)");
        script.ShouldContain("exit 1");
        script.ShouldContain("UseShellExecute = $true");
    }
}