using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SessionIdShould
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("--dangerously-skip-permissions")]
    [InlineData("-x")]
    [InlineData(@"..\..\Northwind")]
    [InlineData("a/b")]
    [InlineData("a b")]
    [InlineData("sessión")]
    public void RejectTextThatIsNotTheCliShapeOfAnId(string text)
    {
        Should.Throw<ArgumentException>(() => SessionId.From(text));
        SessionId.TryParse(text, out SessionId? id).ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Fact]
    public void KeepTheTextAndReadAsIt()
    {
        SessionId id = SessionId.From("1a4d2d86-8e87-44fe-a9f7-61d47d481ae3");

        id.Value.ShouldBe("1a4d2d86-8e87-44fe-a9f7-61d47d481ae3");
        $"{id}".ShouldBe("1a4d2d86-8e87-44fe-a9f7-61d47d481ae3");
        SessionId.TryParse("1a4d2d86-8e87-44fe-a9f7-61d47d481ae3", out SessionId? parsed).ShouldBeTrue();
        parsed.ShouldBe(id);
    }

    [Fact]
    public void AcceptTheCliShapeOfAnId()
    {
        SessionId.TryParse("81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6", out SessionId? id).ShouldBeTrue();
        id.ShouldBe(SessionId.From("81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6"));
    }
}