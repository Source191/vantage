using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class JobIdShould
{
    [Theory]
    [InlineData("81cdb0f9")]
    [InlineData("1A4D2D86")]
    public void BeEightHexCharacters(string text)
    {
        JobId.From(text).Value.ShouldBe(text);
        JobId.TryParse(text, out JobId? id).ShouldBeTrue();
        id.ShouldBe(JobId.From(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("81cdb0f")]
    [InlineData("81cdb0f9a")]
    [InlineData("sess0000")]
    [InlineData("81cd b0f")]
    public void RejectAnythingElse(string text)
    {
        Should.Throw<ArgumentException>(() => JobId.From(text));
        JobId.TryParse(text, out JobId? id).ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Fact]
    public void DeriveFromASessionId()
    {
        JobId.TryParse(SessionId.From("1a4d2d86-8e87-44fe-a9f7-61d47d481ae3"), out JobId? id).ShouldBeTrue();
        id.ShouldBe(JobId.From("1a4d2d86"));
        JobId.TryParse(SessionId.From("not-hex-at-all"), out JobId? notHex).ShouldBeFalse();
        notHex.ShouldBeNull();
        JobId.TryParse(SessionId.From("1a4d"), out JobId? tooShort).ShouldBeFalse();
        tooShort.ShouldBeNull();
    }

    [Fact]
    public void ReadAsItsText()
    {
        $"{JobId.From("81cdb0f9")}".ShouldBe("81cdb0f9");
    }
}