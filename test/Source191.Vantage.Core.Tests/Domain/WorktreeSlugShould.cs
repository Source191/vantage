using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class WorktreeSlugShould
{
    [Theory]
    [InlineData("Refund retry policy", "refund-retry-policy")]
    [InlineData("Fix #123: flaky tests!", "fix-123-flaky-tests")]
    [InlineData("--Trim me--", "trim-me")]
    public void DeriveTheSlugFromAName(string text, string expected)
    {
        WorktreeSlug.TryParse(text, out WorktreeSlug? slug).ShouldBeTrue();
        slug.Value.ShouldBe(expected);
        new WorktreeSlug(text).ShouldBe(slug);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("!!!")]
    public void HaveNoSlugForTextWithNoLettersOrDigits(string? text)
    {
        WorktreeSlug.TryParse(text, out WorktreeSlug? slug).ShouldBeFalse();
        slug.ShouldBeNull();
    }

    [Fact]
    public void RefuseToBuildASlugFromTextWithNoLettersOrDigits()
    {
        Should.Throw<ArgumentException>(() => new WorktreeSlug("!!!"));
    }
}