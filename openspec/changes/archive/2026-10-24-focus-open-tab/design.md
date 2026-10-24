# Design

## Context

See proposal.md. Each Vantage-opened tab is registered with its shell's process id (`TabRegistration`), `IProcessRegistry.ParentOf` already walks a process to its parent by name, and `wt.exe new-tab --title <name> --suppressApplicationTitle` keeps the tab title equal to the session name. A spike with `System.Windows.Automation` on this machine listed the tab items of the Windows Terminal window by name and `SelectionItemPattern.Select()` worked.

## Goals / Non-Goals

**Goals:** one new boundary call, everything else tested with the existing fakes.

**Non-Goals:** bringing another Windows Terminal window to the front; detecting a tab the user renamed.

## Decisions

- **UI Automation through `Interop.UIAutomationClient`** (the COM interfaces, `netstandard2.0`, no desktop framework reference), not FlaUI and not `Microsoft.WindowsDesktop.App`: the latter would move every project to `net10.0-windows` and require the desktop runtime; FlaUI is a larger dependency for one `FindFirst` and one `Select`. Build verified clean under warnings-as-errors.
- **Which window:** walk `ParentOf` from the registered shell at most five steps until the name is `WindowsTerminal`; search that process's top-level windows. If the walk fails (a pending registration has no process yet), search every Windows Terminal window. First tab item whose name equals the session name wins.
- **Seam:** `IWindowsTerminal.FocusTabAsync(int? terminalProcessId, string title, ct)` returns whether a tab was selected. `TabOpener.OpenAsync(SessionRow)` keeps the live-tab check and turns it into the focus call, so the screen is unchanged apart from the reducer no longer refusing `o`.
- **Threading:** the UI Automation call runs where the tab opener already runs, off the Terminal.Gui thread, so a slow query only delays the footer.

## Risks / Trade-offs

- [A session named like an unrelated tab in the same window] → the first match wins; names are the user's own labels and tabs Vantage opens are the usual case.
- [UI Automation unavailable, such as under the headless check's pseudo-console] → `FocusTabAsync` returns false and the footer explains; nothing throws to the screen.
