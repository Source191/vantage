## 1. The icon files

- [x] 1.1 Write `tools\New-Icon.ps1`: draws the Branches icon (stem join tidied so the stem ends inside the lower node's ring), writes `assets\icon\vantage-{16,32,48,64,128,256,512}.png` and `assets\icon\vantage.ico`; the script fails on a wrong PNG size or an ICO that does not list the six sizes up to 256 px (the format cannot hold 512); run it, then look at the 512 and 24 px versions
- [x] 1.2 Copy `vantage-128.png` over `src\Source191.Vantage\icon.png`; test `IconAssetsShould.KeepThePackageIconInStepWithTheAssetFolder` (Infrastructure.Tests, bytes equal) is red before the copy and green after

## 2. The terminal fragment carries the icon

- [x] 2.1 `TerminalFragmentShould.GiveBothProfilesTheCopiedIcon`: with a temporary icon source, `WriteAsync` copies it into the fragment folder and both profiles' `icon` is that path; then `TerminalFragment` takes the icon source and writes it
- [x] 2.2 `TerminalFragmentShould.WriteNoIconWhenTheToolHasNone` (null source and missing file): profiles have no `icon` property and `WriteAsync` does not throw
- [x] 2.3 `TerminalFragmentShould.RemoveTheIconWithTheFragment`: `RemoveAsync` leaves no icon file; likely green at once, so confirm it
- [x] 2.4 Csproj copies and packs `vantage.ico` into the tool folder; `Program.cs` passes `Path.Combine(AppContext.BaseDirectory, "vantage.ico")` to `TerminalFragment.ForCurrentUser`; verify with `tools\install-local.ps1` and by listing the nupkg for `tools\net10.0\any\vantage.ico`

## 3. README, version and documents

- [x] 3.1 README: banner image at the top (128 px, absolute URL), and the Windows Terminal setup section says both profiles carry the icon, that it shows only in tabs opened from them, and that running `vantage setup-terminal` again refreshes it; check in a Markdown preview
- [x] 3.2 Raise `Version` in the csproj to 0.16.0; `.github\scripts\Assert-VersionBumped.ps1` passes
- [x] 3.3 `CLAUDE.md` Windows Terminal facts: the fragment copies `vantage.ico` into its folder and the profiles carry `icon`; record whether a fresh `--profile` tab shows it once checked
- [x] 3.4 Full loop: `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes`, `openspec validate vantage-icon`; archive the change in the same pull request

## Workflow follow-up

- Manual check after `tools\install-local.ps1`: run `vantage setup-terminal`, restart Windows Terminal, open the "Vantage" profile and press `o` on a session; both tabs should show the icon. Record the result for the unverified `--profile` note.
