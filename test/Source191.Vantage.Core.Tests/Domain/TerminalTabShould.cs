using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class TerminalTabShould
{
    private static readonly DateTimeOffset Noon = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FindTheShellWhoseParentIsWindowsTerminalByWalkingUpFromClaude()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry()
            .InTree(1, "WindowsTerminal", 0, Noon)
            .InTree(4000, "pwsh", 1, Noon.AddMinutes(1))
            .InTree(4100, "cmd", 4000, Noon.AddMinutes(2))
            .InTree(4242, "claude", 4100, Noon.AddMinutes(3));

        TerminalTab.ShellOf(4242, processes).ShouldBe(4000);
    }

    [Fact]
    public void TreatClaudeItselfAsTheTabRootWhenWindowsTerminalStartedItDirectly()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(1, "WindowsTerminal", 0, Noon).InTree(4242, "claude", 1, Noon.AddMinutes(1));

        TerminalTab.ShellOf(4242, processes).ShouldBe(4242);
    }

    [Fact]
    public void GiveNothingWhenNoAncestorIsWindowsTerminal()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(7, "explorer", 0, Noon).InTree(4000, "pwsh", 7, Noon.AddMinutes(1)).InTree(4242, "claude", 4000, Noon.AddMinutes(2));

        TerminalTab.ShellOf(4242, processes).ShouldBeNull();
        TerminalTab.ShellOf(9999, processes).ShouldBeNull();
    }

    [Fact]
    public void FindWindowsTerminalWhateverItsCaseAndUpToEightStepsAway()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(1, "windowsterminal", 0, Noon).InTree(4000, "pwsh", 1, Noon.AddMinutes(1));
        for (int process = 4001; process <= 4007; process++)
        {
            processes.InTree(process, "node", process - 1, Noon.AddMinutes(process - 3999));
        }

        TerminalTab.ShellOf(4007, processes).ShouldBe(4000);
    }

    [Fact]
    public void StopAtAParentYoungerThanItsChild()
    {
        FakeProcessRegistry reused = new FakeProcessRegistry()
            .InTree(1, "WindowsTerminal", 0, Noon)
            .InTree(4000, "pwsh", 1, Noon.AddMinutes(5))
            .InTree(4242, "claude", 4000, Noon.AddMinutes(1));

        TerminalTab.ShellOf(4242, reused).ShouldBeNull();
        TerminalTab.TerminalOf(4242, reused).ShouldBeNull();
    }

    [Fact]
    public void StopAtAParentWhoseStartCannotBeRead()
    {
        FakeProcessRegistry unknown = new FakeProcessRegistry()
            .InTree(1, "WindowsTerminal", 0, Noon)
            .InTree(4000, "pwsh", 1)
            .InTree(4242, "claude", 4000, Noon.AddMinutes(1));

        TerminalTab.ShellOf(4242, unknown).ShouldBeNull();
    }

    [Fact]
    public void AnswerTheShellWhenEveryAncestorStartedBeforeItsChild()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry()
            .InTree(1, "WindowsTerminal", 0, Noon)
            .InTree(4000, "pwsh", 1, Noon.AddMinutes(1))
            .InTree(4242, "claude", 4000, Noon.AddMinutes(1));

        TerminalTab.ShellOf(4242, processes).ShouldBe(4000);
        TerminalTab.TerminalOf(4242, processes).ShouldBe(1);
    }
}