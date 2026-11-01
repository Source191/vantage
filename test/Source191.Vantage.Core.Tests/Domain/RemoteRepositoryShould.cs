using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class RemoteRepositoryShould
{
    [Theory]
    [InlineData("https://github.com/northwind/Northwind.Orders")]
    [InlineData("https://github.com/northwind/Northwind.Orders.git")]
    [InlineData("https://github.com/northwind/Northwind.Orders/")]
    [InlineData("git@github.com:northwind/Northwind.Orders.git")]
    [InlineData("git@github.com:northwind/Northwind.Orders")]
    [InlineData("ssh://git@github.com/northwind/Northwind.Orders.git")]
    [InlineData("  https://github.com/northwind/Northwind.Orders.git\n")]
    public void ParseGitHubHttpsAndSshRemotes(string url)
    {
        RemoteRepository.TryParse(url, out RemoteRepository? repository).ShouldBeTrue();
        repository.ShouldBe(new GitHubRepository("northwind", "Northwind.Orders"));
    }

    [Theory]
    [InlineData("https://northwind@dev.azure.com/northwind/Trading/_git/Northwind.Orders")]
    [InlineData("https://dev.azure.com/northwind/Trading/_git/Northwind.Orders")]
    [InlineData("https://northwind.visualstudio.com/Trading/_git/Northwind.Orders")]
    [InlineData("https://northwind.visualstudio.com/DefaultCollection/Trading/_git/Northwind.Orders")]
    [InlineData("git@ssh.dev.azure.com:v3/northwind/Trading/Northwind.Orders")]
    [InlineData("northwind@vs-ssh.visualstudio.com:v3/northwind/Trading/Northwind.Orders")]
    public void ParseAzureDevOpsRemotes(string url)
    {
        RemoteRepository.TryParse(url, out RemoteRepository? repository).ShouldBeTrue();
        repository.ShouldBe(new AzureDevOpsRepository("northwind", "Trading", "Northwind.Orders"));
    }

    [Theory]
    [InlineData("https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders")]
    [InlineData("git@ssh.dev.azure.com:v3/northwind/Northwind%20Trading/Northwind.Orders")]
    public void DecodeEscapedProjectNames(string url)
    {
        RemoteRepository.TryParse(url, out RemoteRepository? repository).ShouldBeTrue();
        repository.ShouldBe(new AzureDevOpsRepository("northwind", "Northwind Trading", "Northwind.Orders"));
    }

    [Fact]
    public void NameItsHost()
    {
        new GitHubRepository("northwind", "Northwind.Orders").Host.ShouldBe(PullRequestHost.GitHub);
        new AzureDevOpsRepository("northwind", "Trading", "Northwind.Orders").Host.ShouldBe(PullRequestHost.AzureDevOps);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://gitlab.com/northwind/Northwind.Orders.git")]
    [InlineData(@"C:\repos\Northwind.Orders")]
    [InlineData("https://github.com/northwind")]
    [InlineData("https://dev.azure.com/northwind/Trading&Co/_git/Northwind.Orders")]
    [InlineData("https://dev.azure.com/northwind/Trading%26Co/_git/Northwind.Orders")]
    [InlineData("https://dev.azure.com/northwind/Trading|Co/_git/Northwind.Orders")]
    [InlineData("https://dev.azure.com/northwind/Trading/_git/Northwind^Orders")]
    [InlineData("git@ssh.dev.azure.com:v3/north%25wind/Trading/Northwind.Orders")]
    public void AnswerFalseForOtherHostsBlankTextAndUnsafeAzureNames(string? url)
    {
        RemoteRepository.TryParse(url, out RemoteRepository? repository).ShouldBeFalse();
        repository.ShouldBeNull();
    }
}