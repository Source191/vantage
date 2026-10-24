# Tasks

## 1. Boundary and tab opener

- [x] 1.1 `IWindowsTerminal.FocusTabAsync`; `FakeWindowsTerminal` records (terminalProcessId, title) and answers a configurable result; verify `dotnet build -c Debug`
- [x] 1.2 TabOpenerShould: SwitchToTheLiveTabInsteadOfOpeningAnother (registration claimed by shell 4242 whose parent chain reaches WindowsTerminal 777; the fake gets (777, "Refund retry policy"); footer "Switched to the tab of Refund retry policy."); verify it passes
- [x] 1.3 TabOpenerShould: SearchEveryWindowWhenTheTabShellHasNoKnownParent (pending registration: focus called with null process id) and SayWhenTheLiveTabCannotBeFound (fake answers false: footer names the problem, no tab opened); verify they pass

## 2. Reducer

- [x] 2.1 SessionsViewShould: split RefuseEnterAndOpenInTabWhenALiveTabAlreadyShowsTheSession into Enter refusing (footer names o) and `o` emitting OpenTabForSession on a live tab; verify SessionsViewShould passes

## 3. Adapter

- [x] 3.1 `WindowsTerminal.FocusTabAsync` with `Interop.UIAutomationClient`: windows of the terminal process (or all WindowsTerminal processes), descendant tab item by name, SelectionItemPattern Select; verify `dotnet build`, then by hand: `o` on a row whose tab is open switches to it

## 4. Text

- [x] 4.1 README (open-in-tab paragraph, "keep one session in its own tab" scenario, manual checklist step), HelpText "o open in its own tab (switches to it when open)", `openspec\config.yaml` context; verify `grep "refus"` in README and Screens\Tui finds only intended lines

## Workflow follow-up

- Archive the change (`openspec archive focus-open-tab -y`).
