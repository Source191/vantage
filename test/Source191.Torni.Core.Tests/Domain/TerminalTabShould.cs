using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Domain;

public sealed class TerminalTabShould
{
    [Fact]
    public void FindTheShellWhoseParentIsWindowsTerminalByWalkingUpFromClaude()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry()
            .InTree(1, "WindowsTerminal", 0, Fixtures.Noon)
            .InTree(4000, "pwsh", 1, Fixtures.Noon.AddMinutes(1))
            .InTree(4100, "cmd", 4000, Fixtures.Noon.AddMinutes(2))
            .InTree(4242, "claude", 4100, Fixtures.Noon.AddMinutes(3));

        ShellIdAbove(4242, processes).ShouldBe(4000);
    }

    [Fact]
    public void AnswerTheProcessItselfWhenWindowsTerminalStartedItDirectly()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(1, "WindowsTerminal", 0, Fixtures.Noon).InTree(4242, "claude", 1, Fixtures.Noon.AddMinutes(1));

        TerminalTab.ShellAbove(TestProcesses.Confirmed(processes, 4242), processes).ShouldBe(new TabShell.TheProcess());
    }

    [Fact]
    public void AnswerNoneWhenNoAncestorIsWindowsTerminal()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(7, "explorer", 0, Fixtures.Noon).InTree(4000, "pwsh", 7, Fixtures.Noon.AddMinutes(1)).InTree(4242, "claude", 4000, Fixtures.Noon.AddMinutes(2));

        TerminalTab.ShellAbove(TestProcesses.Confirmed(processes, 4242), processes).ShouldBe(new TabShell.None());
        TerminalTab.ShellAbove(TestProcesses.Confirmed(9999, Fixtures.Noon), processes).ShouldBe(new TabShell.None());
    }

    [Fact]
    public void FindWindowsTerminalWhateverItsCaseAndUpToEightStepsAway()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(1, "windowsterminal", 0, Fixtures.Noon).InTree(4000, "pwsh", 1, Fixtures.Noon.AddMinutes(1));
        for (int process = 4001; process <= 4007; process++)
        {
            processes.InTree(process, "node", process - 1, Fixtures.Noon.AddMinutes(process - 3999));
        }

        ShellIdAbove(4007, processes).ShouldBe(4000);
    }

    [Fact]
    public void StopAtAParentYoungerThanItsChild()
    {
        FakeProcessRegistry reused = new FakeProcessRegistry()
            .InTree(1, "WindowsTerminal", 0, Fixtures.Noon)
            .InTree(4000, "pwsh", 1, Fixtures.Noon.AddMinutes(5))
            .InTree(4242, "claude", 4000, Fixtures.Noon.AddMinutes(1));

        TerminalTab.ShellAbove(TestProcesses.Confirmed(reused, 4242), reused).ShouldBe(new TabShell.None());
        TerminalTab.TerminalOf(TestProcesses.Confirmed(reused, 4242), reused).ShouldBeNull();
    }

    [Fact]
    public void StopAtAParentWhoseStartCannotBeRead()
    {
        FakeProcessRegistry unknown = new FakeProcessRegistry()
            .InTree(1, "WindowsTerminal", 0, Fixtures.Noon)
            .InTree(4000, "pwsh", 1)
            .InTree(4242, "claude", 4000, Fixtures.Noon.AddMinutes(1));

        TerminalTab.ShellAbove(TestProcesses.Confirmed(unknown, 4242), unknown).ShouldBe(new TabShell.None());
    }

    [Fact]
    public void AnswerTheShellWhenEveryAncestorStartedBeforeItsChild()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry()
            .InTree(1, "WindowsTerminal", 0, Fixtures.Noon)
            .InTree(4000, "pwsh", 1, Fixtures.Noon.AddMinutes(1))
            .InTree(4242, "claude", 4000, Fixtures.Noon.AddMinutes(1));

        ShellIdAbove(4242, processes).ShouldBe(4000);
        TerminalTab.TerminalOf(TestProcesses.Confirmed(processes, 4242), processes).ShouldBe(1);
    }

    private static int ShellIdAbove(int processId, FakeProcessRegistry processes)
    {
        TabShell shell = TerminalTab.ShellAbove(TestProcesses.Confirmed(processes, processId), processes);

        return shell.ShouldBeOfType<TabShell.Above>().Shell.Id;
    }
}