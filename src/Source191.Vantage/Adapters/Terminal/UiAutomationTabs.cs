using Interop.UIAutomationClient;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// Selects a Windows Terminal tab by its title through UI Automation, because the terminal's own command line can
/// only focus a tab by index. Vantage titles the tabs it opens with the session name and suppresses title changes,
/// so the title is the key. Searches the window of a known terminal process, or every Windows Terminal window.
/// </summary>
internal static class UiAutomationTabs
{
    private const string TerminalProcessName = "WindowsTerminal";

    public static bool Select(int? terminalProcessId, string title)
    {
        try
        {
            IUIAutomation automation = new CUIAutomation8();
            IUIAutomationElement desktop = automation.GetRootElement();

            return TerminalProcesses(terminalProcessId).Any(processId => SelectIn(automation, desktop, processId, title));
        }
        catch (COMException)
        {
            return false;
        }
    }

    private static List<int> TerminalProcesses(int? known)
    {
        if (known is int processId)
        {
            return [processId];
        }

        List<int> found = [];
        foreach (Process process in Process.GetProcessesByName(TerminalProcessName))
        {
            using (process)
            {
                found.Add(process.Id);
            }
        }

        return found;
    }

    private static bool SelectIn(IUIAutomation automation, IUIAutomationElement desktop, int processId, string title)
    {
        IUIAutomationCondition ownedWindow = automation.CreatePropertyCondition(UIA_PropertyIds.UIA_ProcessIdPropertyId, processId);
        IUIAutomationCondition titledTab = automation.CreateAndCondition(
            automation.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_TabItemControlTypeId),
            automation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, title));
        IUIAutomationElementArray windows = desktop.FindAll(TreeScope.TreeScope_Children, ownedWindow);
        for (int index = 0; index < windows.Length; index++)
        {
            IUIAutomationElement? tab = windows.GetElement(index).FindFirst(TreeScope.TreeScope_Descendants, titledTab);
            if (tab?.GetCurrentPattern(UIA_PatternIds.UIA_SelectionItemPatternId) is IUIAutomationSelectionItemPattern selection)
            {
                selection.Select();
                return true;
            }
        }

        return false;
    }
}