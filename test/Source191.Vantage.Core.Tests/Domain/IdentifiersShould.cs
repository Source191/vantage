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

public sealed class BranchNameShould
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("fix 123")]
    [InlineData("fix\t123")]
    public void RejectBlankAndWhitespace(string text)
    {
        Should.Throw<ArgumentException>(() => BranchName.From(text));
        BranchName.TryParse(text, out BranchName? branch).ShouldBeFalse();
        branch.ShouldBeNull();
    }

    [Fact]
    public void KeepTheTextAndReadAsIt()
    {
        BranchName branch = BranchName.From("fix/flaky-tests");

        branch.Value.ShouldBe("fix/flaky-tests");
        $"{branch}".ShouldBe("fix/flaky-tests");
        BranchName.TryParse("fix/flaky-tests", out BranchName? parsed).ShouldBeTrue();
        parsed.ShouldBe(branch);
    }
}
