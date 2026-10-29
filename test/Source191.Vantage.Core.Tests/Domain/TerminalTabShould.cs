using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class TerminalTabShould
{
    [Fact]
    public void FindTheShellWhoseParentIsWindowsTerminalByWalkingUpFromClaude()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry()
            .InTree(1, "WindowsTerminal", 0)
            .InTree(4000, "pwsh", 1)
            .InTree(4100, "cmd", 4000)
            .InTree(4242, "claude", 4100);

        TerminalTab.ShellOf(4242, processes).ShouldBe(4000);
    }

    [Fact]
    public void TreatClaudeItselfAsTheTabRootWhenWindowsTerminalStartedItDirectly()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(1, "WindowsTerminal", 0).InTree(4242, "claude", 1);

        TerminalTab.ShellOf(4242, processes).ShouldBe(4242);
    }

    [Fact]
    public void GiveNothingWhenNoAncestorIsWindowsTerminal()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(7, "explorer", 0).InTree(4000, "pwsh", 7).InTree(4242, "claude", 4000);

        TerminalTab.ShellOf(4242, processes).ShouldBeNull();
        TerminalTab.ShellOf(9999, processes).ShouldBeNull();
    }

    [Fact]
    public void FindWindowsTerminalWhateverItsCaseAndUpToEightStepsAway()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(1, "windowsterminal", 0).InTree(4000, "pwsh", 1);
        for (int process = 4001; process <= 4007; process++)
        {
            processes.InTree(process, "node", process - 1);
        }

        TerminalTab.ShellOf(4007, processes).ShouldBe(4000);
    }
}