using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class CommandArgumentsShould
{
    [Fact]
    public void SplitOnSpaces()
    {
        CommandArguments.Split("-n  --wait {folder}").ShouldBe(["-n", "--wait", "{folder}"]);
    }

    [Fact]
    public void KeepDoubleQuotedTextTogether()
    {
        CommandArguments.Split("--title \"My Project\" {folder}").ShouldBe(["--title", "My Project", "{folder}"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void KeepAnEmptyLineEmpty(string line)
    {
        CommandArguments.Split(line).ShouldBeEmpty();
    }

    [Fact]
    public void JoinQuotingAnArgumentWithASpace()
    {
        CommandArguments.Join(["--title", "My Project", "{folder}"]).ShouldBe("--title \"My Project\" {folder}");
    }

    [Fact]
    public void SplitWhatItJoined()
    {
        IReadOnlyList<string> arguments = ["-n", "a b", "{folder}"];

        CommandArguments.Split(CommandArguments.Join(arguments)).ShouldBe(arguments);
    }
}