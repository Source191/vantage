# Proposal

## Why

Pressing `o` on a session whose Vantage-opened tab is still alive only prints "already open in another tab", so the user has to find the tab by eye among many. Windows Terminal's command line cannot focus a tab by name, but Windows UI Automation can select a tab item by its title in the owning Windows Terminal window, and Vantage titles every tab it opens with the session name.

## What Changes

- `o` on a session with a live Vantage-opened tab switches to that tab instead of refusing; the footer says which tab it switched to. Enter still refuses, and now names `o` as the way to get there.
- Vantage finds the tab by walking from the registered tab shell's process up to its `WindowsTerminal.exe` parent and selecting the tab item whose title is the session name through UI Automation. When no such tab item can be found (another window, a renamed tab, UI Automation unavailable) the footer says so and nothing else happens.
- A new dependency, `Interop.UIAutomationClient`, is added to the tool for the UI Automation COM interfaces.
- The project constraint "do not design features that need to find an existing tab" is relaxed: the terminal's command line cannot, UI Automation can.

Out of scope: focusing a Windows Terminal window that is not in the foreground (the tab is selected in whichever window owns it), Enter jumping to a tab, multiple Claude accounts.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: the "Open a session in its own Windows Terminal tab" requirement's live-tab case changes from refusing to switching to the tab.

## Impact

- `Adapters\Terminal\IWindowsTerminal` gains a focus-tab call; `WindowsTerminal` implements it with UI Automation; the test double records it.
- `Actions\TabOpener` resolves the live registration to its terminal window and asks for the focus.
- `Screens\SessionsView` stops refusing `o` on a live tab; its tests split.
- README, help text, `openspec\config.yaml` context.
