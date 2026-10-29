using Source191.Vantage.Application.Diagnostics;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class ClaudeVersionShould
{
    [Fact]
    public void ReadTheLeadingNumber()
    {
        ClaudeVersion.TryParse("2.1.310", out Version? version).ShouldBeTrue();

        version.ShouldBe(new Version(2, 1, 310));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void RefuseTextThatIsNotAVersion(string? text)
    {
        ClaudeVersion.TryParse(text, out Version? version).ShouldBeFalse();

        version.ShouldBeNull();
    }
}
