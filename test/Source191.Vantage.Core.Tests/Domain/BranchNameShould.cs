using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

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