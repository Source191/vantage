## Context

See proposal.md for motivation. Today `TerminalFragment` (Infrastructure) writes `Fragments\Vantage\vantage.json` with two profiles and `RemoveAsync` deletes the whole `Fragments\Vantage` folder. Windows Terminal draws a tab's icon from its profile's `icon` property (an absolute path to a PNG, ICO or similar, or an emoji). `wt new-tab` has no icon option and there is no escape sequence for one, so only tabs opened from one of the two profiles can show it. The package icon (`icon.png`) is packed at the package root, outside the tool's `tools\net10.0\any` folder, so the running tool cannot read it.

## Goals / Non-Goals

**Goals:** one source of truth for the icon at every size, regenerable; the package, README and Windows Terminal profiles all show it; setup and remove stay a single folder's worth of files.

**Non-Goals:** icons inside Terminal.Gui; making a tab started from another profile show the icon; a new port or setting.

## Decisions

- **Draw once, in PowerShell, check in the output.** `tools\New-Icon.ps1` (GDI+, no dependencies, matching the throwaway script that produced the options) renders a 512 px master and downscales to each size, then assembles `vantage.ico` from the PNG sizes (ICO accepts PNG-compressed entries). The PNGs and ICO are committed so a build never needs the script. Alternative: hand-edited images with no source; rejected because the tidy-up and any later colour change would be unrepeatable.
- **Files in `assets\icon\`, named `vantage-<size>.png` and `vantage.ico`.** `src\Source191.Vantage\icon.png` stays the package icon as the csproj and NuGet expect (a copy of `vantage-128.png`); a test in the Infrastructure tests compares the two files' bytes, since drift would otherwise go unnoticed.
- **Ship `vantage.ico` in the tool, not `icon.png`.** The csproj includes `..\..\assets\icon\vantage.ico` as content copied to the output, which packs it into `tools\net10.0\any` beside `vantage.dll`. `Program.cs` hands `TerminalFragment` `Path.Combine(AppContext.BaseDirectory, "vantage.ico")`. Alternative: an embedded resource written out at setup; rejected as more code for the same result.
- **Copy the icon into the fragment folder, not `%LOCALAPPDATA%\Vantage`.** A global tool's folder changes with every version, so a profile pointing into it would break after `dotnet tool update`; the copy gives Windows Terminal a stable path, and `RemoveAsync` already deletes the folder, so no new remove step or port is needed. Running `vantage setup-terminal` again refreshes the copy. This replaces the earlier idea of `%LOCALAPPDATA%\Vantage\icon.png`.
- **The adapter owns it.** `TerminalFragment` takes the icon source path as a second constructor parameter (null or a missing file means no icon), copies it in `WriteAsync` and adds `["icon"]` with the copied path to both profiles. `TerminalProfiles`, `ITerminalFragment`, the runner and `Wording` do not change, because the plan line already names the fragment's location.
- **README image by absolute URL.** `https://raw.githubusercontent.com/Source191/vantage/main/assets/icon/vantage-128.png`, because nuget.org renders only absolute URLs from trusted hosts in a package readme. It shows broken until the repository is public and this change is on `main`, which the public-release change already waits for.

## Risks / Trade-offs

- [`wt` may not show the icon on a tab opened with `--profile` straight after setup, before Windows Terminal has loaded the fragment] → the existing unverified `--profile` note covers it; check by hand after a restart and record the result.
- [The main tab shows the icon only when started from the "Vantage" profile] → documented in the README; accepted for this experiment.
- [The ICO is assembled by script] → the script fails if the entry count or sizes are wrong, and the manual check opens it in Windows Terminal.
- [The packaged icon and `assets\icon\vantage-128.png` drift] → the byte-comparison test.
