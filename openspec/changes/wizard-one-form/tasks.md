# Tasks

## 1. The one-column layout (Tui)

- [x] 1.1 `WizardControls`: the chooser (caption, filter, list with `Dim.Fill` over the rows beneath, floor 4) above the form at one x, the form rows following the design's order, the fixed-folder layout unchanged. `LaunchWizardDialog`: the full dialog 80 wide and nine tenths tall. Verify `dotnet build -c Debug` is clean and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` passes (no rule changed).
- [x] 1.2 Verify with `dotnet run --project tools\HeadlessTerminal -- drive` at 120 x 36 against roots holding at least twelve repositories: the capture (`grid`) shows the filter above the list, at least four repositories, the name, Where, the base, the preview, the prompt, the hint and the three buttons, all within the dialog; typing a letter in the filter narrows the list; Enter in the prompt adds a line and the dialog stays. Then `s` on a worktree row shows the compact form as before. Record the captures here.
  - Done 30 October 2026, headless at 120 x 36 against roots holding dozens of repositories: one 80-cell column with the caption, the filter, eight repositories, the name, Where, the base, a two-line preview, the prompt, the problem line, the hint and the three buttons, all inside the dialog (the preview first had one row and was cut, so the form takes seventeen rows). Typing MK.S in the filter narrows the list to the MK.Sales repositories. Enter in the prompt puts world on a new line and the dialog stays; hello scrolls out of the three-row box, and the wizard on main does the same with its five rows, so that is not this change. s on a worktree row opens the compact form as before (Directory, name, preview, prompt, Enter starts and opens in tab).
- [x] 1.3 README: the wizard section's first paragraph describes the one column (the repository list above the form). Verify by reading it against the capture.

## 2. Version and checks

- [x] 2.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.
  - Done 30 October 2026: 0.18.0-preview.9 (main is 0.18.0-preview.8); no warnings, 1839 tests pass (seven contract tests skip), format passes.
- [x] 2.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal` and the smoke check against the packed tool. Verify both exit 0.
  - Done 30 October 2026: the hand-off check exits 0 and the smoke check (which opens and cancels the wizard and the compact form) exits 0 against the packed tool, Northwind.Smoke seeded and removed.
- [x] 2.3 The product owner installs the build with `tools\install-local.ps1` and, on the laptop screen, opens the wizard, filters, chooses a repository, tabs down the form, presses Enter in the prompt, presses F6, and opens the compact form from a worktree row. Record here.
  - Recorded 30 October 2026: the owner installed 0.18.0-preview.9-local.20261030215628, checked the one-column wizard and the compact form on his terminal, and asked for the pull request.

## Workflow follow-up

- Archive after tabs-and-key-bar, in its own pull request, syncing the session-launch delta into `openspec\specs`.
