# Proposal

## Why

Vantage is installed with `dotnet tool install -g Source191.Vantage`, which needs the .NET 10 SDK (not just the runtime), and updated with `dotnet tool update`, which fails while any Vantage runs. A new user must install five programs by hand before the first start. winget can install a self-contained Vantage, bring in the prerequisites it can (Windows Terminal, PowerShell 7, git) and upgrade it with one command, as it already does for k9s and most Windows command-line tools.

## What Changes

- Stable releases are installed with `winget install Source191.Vantage`. The package is a portable zip of a self-contained, single-file `win-x64` build, so the .NET SDK and runtime are no longer prerequisites. The manifest declares `Microsoft.WindowsTerminal`, `Microsoft.PowerShell` and `Git.Git` as package dependencies.
- Claude Code stays a manual prerequisite: winget's `Anthropic.ClaudeCode` is at 2.1.302, below the 2.1.309 Vantage needs, and does not update itself, so it cannot be a dependency. The README keeps the native installer line.
- Every release carries the zip as a GitHub Release asset, built and attested by `release.yml`. A new step submits each stable version to `microsoft/winget-pkgs` (first submission by hand, later ones with `wingetcreate update` and a token).
- Previews are no longer installed from nuget.org: they are the CI artefact zip (and the zip on the preview's GitHub prerelease), unzipped by hand or by `tools\install-local.ps1 -FromArtifact`. winget never sees a prerelease.
- The update check reads the published versions from `microsoft/winget-pkgs` (`manifests/s/Source191/Vantage`) instead of nuget.org, so Vantage never offers a version `winget upgrade` cannot yet install. The release notes come from `CHANGELOG.md` at that version's tag on GitHub. `U` opens the update tab, which waits for Vantage to exit, runs `winget upgrade` and starts Vantage again.
- A Vantage that was installed as a dotnet tool tells the user to move to winget, and `U` does it: `winget install`, then `dotnet tool uninstall -g Source191.Vantage`, then start the new Vantage. This is the one last release published to nuget.org; the package is then deprecated there.
- `tools\install-local.ps1`, the pull request check's smoke step and the README, CONTRIBUTING and MAINTAINING move from the nupkg and `dotnet tool` to the zip and winget.
- **BREAKING:** nuget.org stops being how Vantage is installed or updated. A dotnet tool install still runs until the user moves, but it never sees a release after the migration one.

## Capabilities

### New Capabilities
- `installation`: how Vantage is installed, what it needs first, how a release becomes a winget package, how previews are tried, and how a dotnet tool install moves to winget.

### Modified Capabilities
- `tool-updates`: the check reads winget-pkgs and GitHub instead of nuget.org, and the update tab runs `winget upgrade`.
- `release-notes`: the notes come from the changelog at the release's tag on GitHub, not from the nuspec; the package no longer carries them; a stable GitHub Release also carries the zip.
- `config-tab`: the Last update check row names the update source in place of nuget.org.
- `command-line`: the `--version` scenario no longer says the version came from nuget.org.

## Impact

- **Code:** `Source191.Vantage.Infrastructure` (a winget-pkgs and GitHub reader replaces `NuGetPackageVersions`), `Application\UpdateCheck`, `Application\TabScripts` (update and migration tabs), `Wording`, the install-channel decision and the tool project's publish settings (`PublishSingleFile`, `SelfContained`, `win-x64`).
- **Workflows:** `release.yml` (zip, attestation, winget step), `check.yml` (smoke against the zip), `build-test-pack`, and `publish.yml`, which is kept for the migration release only.
- **Docs:** README install and prerequisites, CONTRIBUTING, MAINTAINING (winget-pkgs token, first submission, deprecating the nuget.org package), `.claude\rules\checks.md` and `windows.md`.
- **Dependencies:** none added to the tool. The release workflow gains `wingetcreate` (or `komac`) and a repository secret.
- **Size:** the download grows from a small framework-dependent package to about 34 MB zipped.

## Out of scope

- Multiple Claude accounts, which is a later change.
- A `win-arm64` build (the x64 build runs under emulation on Arm Windows; a second zip is a later change if needed).
- Installers other than winget (Scoop, Chocolatey, MSI, the Microsoft Store); the GitHub Release zip is the manual fallback for a machine without winget.
- Code signing. Unsigned builds may meet SmartScreen; signing is a separate decision with its own cost.
- Removing the dotnet tool packaging and `publish.yml`: they stay for the migration release and go in a later change, once nuget.org installs have had time to move.
- Installing or upgrading Claude Code from Vantage.
- macOS or Linux.
