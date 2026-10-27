## Why

The package icon is a placeholder (a white "V" on blue) and Vantage's Windows Terminal tabs show a generic PowerShell icon, so nothing marks them as Vantage's. The icon is chosen (option 3, "Branches": two worktree branches merging into one node, drawn as a V) and wanted before the tool goes public.

## What Changes

- Replace `src\Source191.Vantage\icon.png` (the 128 x 128 package icon) with the Branches icon, with the join between the stem and the lower node tidied.
- Add the icon at 16, 32, 48, 64, 128, 256 and 512 px as PNG, plus one multi-size `vantage.ico`, in `assets\icon\`, with the PowerShell script that draws them (`tools\New-Icon.ps1`) so they can be regenerated rather than edited by hand.
- Show the 128 px icon at the top of the README, by its absolute raw.githubusercontent.com URL so the nuget.org readme renders it too.
- Ship `vantage.ico` beside the tool; `vantage setup-terminal` copies it into the fragment folder and sets `icon` on the "Vantage" and "Vantage session" profiles, so Vantage's tab and every tab it opens show it. `--remove` already deletes the folder, so it removes the icon too.
- Raise `Version` in the csproj (minor: a new visible behaviour that keeps everything else working).
- This is an experiment: the user is not convinced about the Vantage profiles, and an icon only shows on a tab opened from one of them.

Out of scope: multiple Claude accounts (a later change); relaunching `vantage` under its own profile when started from another tab (Windows Terminal has no escape sequence for a tab icon); an icon for the Terminal.Gui screens; a Windows shortcut or installer icon; publishing to nuget.org.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `needs-input-notification`: the requirement that the setup command adds Vantage's Windows Terminal profiles now also gives both profiles the Vantage icon.

## Impact

- `src\Source191.Vantage.Infrastructure\Terminal\TerminalFragment.cs` (icon copy and `icon` property), its `TerminalFragmentShould` tests, `Program.cs` (passes the icon's location).
- `src\Source191.Vantage\Source191.Vantage.csproj` (`vantage.ico` copied to the output and packed, `Version`), `icon.png`.
- New `assets\icon\*` and `tools\New-Icon.ps1`; `README.md` (banner, Windows Terminal setup section).
- `CLAUDE.md` Windows Terminal facts: a fragment may carry an icon; `--profile` before restart is still unverified.
