using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SessionNameShould
{
    [Fact]
    public void RejectABlankName()
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => SessionName.Parse("   "));

        exception.Message.ShouldContain("required");
    }

    [Theory]
    [InlineData("fix; rm -rf")]
    [InlineData("say \"hello\"")]
    public void RejectANameWindowsTerminalCannotPassSafely(string text)
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => SessionName.Parse(text));

        exception.Message.ShouldContain("semicolon or a double quote");
    }

    [Fact]
    public void KeepTheTextExactlyAsTyped()
    {
        SessionName name = SessionName.Parse("Refund retry policy");

        name.Text.ShouldBe("Refund retry policy");
    }

    [Theory]
    [InlineData("Refund retry policy", "refund-retry-policy")]
    [InlineData("Fix #123: flaky tests!", "fix-123-flaky-tests")]
    [InlineData("--Trim me--", "trim-me")]
    public void DeriveAWorktreeSlugFromTheName(string text, string expectedSlug)
    {
        SessionName name = SessionName.Parse(text);

        name.Slug.ShouldNotBeNull();
        name.Slug.Value.ShouldBe(expectedSlug);
    }

    [Fact]
    public void HaveNoSlugWhenTheNameHasNoLettersOrDigits()
    {
        SessionName name = SessionName.Parse("!!!");

        name.Slug.ShouldBeNull();
    }
}