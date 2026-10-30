# Tasks

## 1. The one-column layout (Tui)

- [ ] 1.1 `WizardControls`: the chooser (caption, filter, list with `Dim.Fill` over the rows beneath, floor 4) above the form at one x, the form rows following the design's order, the fixed-folder layout unchanged. `LaunchWizardDialog`: the full dialog 80 wide and nine tenths tall. Verify `dotnet build -c Debug` is clean and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` passes (no rule changed).
- [ ] 1.2 Verify with `dotnet run --project tools\HeadlessTerminal -- drive` at 120 x 36 against roots holding at least twelve repositories: the capture (`grid`) shows the filter above the list, at least four repositories, the name, Where, the base, the preview, the prompt, the hint and the three buttons, all within the dialog; typing a letter in the filter narrows the list; Enter in the prompt adds a line and the dialog stays. Then `s` on a worktree row shows the compact form as before. Record the captures here.
- [ ] 1.3 README: the wizard section's first paragraph describes the one column (the repository list above the form). Verify by reading it against the capture.

## 2. Version and checks

- [ ] 2.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.
- [ ] 2.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal` and the smoke check against the packed tool. Verify both exit 0.
- [ ] 2.3 The product owner installs the build with `tools\install-local.ps1` and, on the laptop screen, opens the wizard, filters, chooses a repository, tabs down the form, presses Enter in the prompt, presses F6, and opens the compact form from a worktree row. Record here.

## Workflow follow-up

- Archive after tabs-and-key-bar, in its own pull request, syncing the session-launch delta into `openspec\specs`.
