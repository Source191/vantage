using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SessionNameShould
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RefuseABlankNameAsMissing(string text)
    {
        SessionName.Validate(text).ShouldBe(new SessionNameValidation.Refused(NameProblem.Missing));
    }

    [Theory]
    [InlineData("fix; rm -rf")]
    [InlineData("say \"hello\"")]
    public void RefuseASemicolonOrDoubleQuoteAsUnsafe(string text)
    {
        SessionName.Validate(text).ShouldBe(new SessionNameValidation.Refused(NameProblem.UnsafeCharacters));
    }

    [Fact]
    public void AcceptAnyOtherName()
    {
        SessionName.Validate("Fix #123: flaky tests!").ShouldBeOfType<SessionNameValidation.Valid>().Name.Value.ShouldBe("Fix #123: flaky tests!");
    }

    [Fact]
    public void CheckBlankBeforeUnsafe()
    {
        SessionName.Validate(" ").ShouldBe(new SessionNameValidation.Refused(NameProblem.Missing));
        SessionName.Validate("; ").ShouldBe(new SessionNameValidation.Refused(NameProblem.UnsafeCharacters));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("fix; rm -rf")]
    public void ReadARefusedNameAsNothingAndRefuseToParseIt(string text)
    {
        SessionName.TryParse(text).ShouldBeNull();
        Should.Throw<ArgumentException>(() => SessionName.Parse(text));
    }

    [Fact]
    public void KeepTheTextExactlyAsTyped()
    {
        SessionName name = SessionName.Parse("Refund retry policy");

        name.Value.ShouldBe("Refund retry policy");
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