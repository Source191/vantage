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
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("fix; rm -rf")]
    public void ReadARefusedNameAsNothing(string? text)
    {
        SessionName.TryParse(text, out SessionName? name).ShouldBeFalse();
        name.ShouldBeNull();
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("fix; rm -rf")]
    [InlineData("say \"hello\"")]
    public void RefuseToBuildANameValidateRefuses(string text)
    {
        Should.Throw<ArgumentException>(() => SessionName.From(text));
    }

    [Fact]
    public void KeepTheTextExactlyAsTyped()
    {
        SessionName name = SessionName.From("Refund retry policy");

        name.Value.ShouldBe("Refund retry policy");
        SessionName.TryParse("Refund retry policy", out SessionName? parsed).ShouldBeTrue();
        parsed.ShouldBe(name);
    }
}