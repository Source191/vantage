# Proposal

## Why

Torni is installed with `dotnet tool install -g Source191.Torni`, which needs the .NET 10 SDK (not just the runtime), and updated with `dotnet tool update`, which fails while any Torni runs. A new user must install five programs by hand before the first start. winget can install a self-contained Torni, bring in the prerequisites it can (Windows Terminal, PowerShell 7, git) and upgrade it with one command, as it already does for k9s and most Windows command-line tools.

## What Changes

- Stable releases are installed with `winget install Source191.Torni`. The package is a portable zip of a self-contained, single-file `win-x64` build, so the .NET SDK and runtime are no longer prerequisites. The manifest declares `Microsoft.WindowsTerminal`, `Microsoft.PowerShell` and `Git.Git` as package dependencies.
- Claude Code stays a manual prerequisite: winget's `Anthropic.ClaudeCode` is at 2.1.302, below the 2.1.309 Torni needs, and does not update itself, so it cannot be a dependency. The README keeps the native installer line.
- Every release carries the zip as a GitHub Release asset, built by `pipeline.yml` and attested by its release job. `publish.yml`, run by the owner once a stable release is tried, submits it to `microsoft/winget-pkgs` (first submission by hand, later ones with `wingetcreate update` and a token held by an approval-gated environment).
- Previews are the CI artefact zip (and the zip on the preview's GitHub prerelease), unzipped by hand or by `tools\install-local.ps1 -FromArtifact`. winget never sees a prerelease.
- The update check reads the published versions from `microsoft/winget-pkgs` (`manifests/s/Source191/Torni`) instead of nuget.org, so Torni never offers a version `winget upgrade` cannot yet install. The release notes come from `CHANGELOG.md` at that version's tag on GitHub. `U` opens the update tab, which waits for Torni to exit, runs `winget upgrade` and starts Torni again.
- The dotnet tool goes: no `PackAsTool`, no nupkg, no nuget.org push in `publish.yml`, no package icon. nuget.org has only previews of it, as `Source191.Vantage`, and a preview never checks for updates, so a migration release could reach nobody; the owner unlists and deprecates the nuget.org package by hand.
- `tools\install-local.ps1`, the pull request check's smoke step and the README, CONTRIBUTING and MAINTAINING move from the nupkg and `dotnet tool` to the zip and winget.
- **BREAKING:** nuget.org stops being how Torni is installed or updated. An installed dotnet tool preview keeps running, but nothing new is published there.

## Capabilities

### New Capabilities
- `installation`: how Torni is installed, what it needs first, how a release becomes a winget package, and how previews are tried.

### Modified Capabilities
- `tool-updates`: the check reads winget-pkgs and GitHub instead of nuget.org, and the update tab runs `winget upgrade`.
- `release-notes`: the notes come from the changelog at the release's tag on GitHub, not from the nuspec; the package no longer carries them; a stable GitHub Release also carries the zip.
- `config-tab`: the Last update check row names the update source in place of nuget.org.
- `command-line`: the `--version` scenario no longer says the version came from nuget.org.

## Impact

- **Code:** `Source191.Torni.Infrastructure` (a winget-pkgs and GitHub reader replaces `NuGetPackageVersions`), `Application\UpdateCheck`, `Application\TabScripts` (the update tab), `Wording`, and the tool project's publish settings (`PublishSingleFile`, `SelfContained`, `win-x64`) in place of its packaging.
- **Workflows:** `pipeline.yml` (smoke against the zip, and the release job's zip and attestation), `build-test-pack` (zip instead of nupkg), and `publish.yml`, which submits a release's zip to winget instead of pushing a nupkg to nuget.org.
- **Docs:** README install and prerequisites, CONTRIBUTING, MAINTAINING (winget-pkgs token, first submission); retiring the nuget.org package is a one-off owner step listed in the pull request, `.claude\rules\checks.md` and `windows.md`.
- **Dependencies:** none added to the tool. The release workflow gains `wingetcreate` (or `komac`) and a repository secret.
- **Size:** the download grows from a small framework-dependent package to about 34 MB zipped.

## Out of scope

- Multiple Claude accounts, which is a later change.
- A `win-arm64` build (the x64 build runs under emulation on Arm Windows; a second zip is a later change if needed).
- Installers other than winget (Scoop, Chocolatey, MSI, the Microsoft Store); the GitHub Release zip is the manual fallback for a machine without winget.
- Code signing. Unsigned builds may meet SmartScreen; signing is a separate decision with its own cost.
- Installing or upgrading Claude Code from Torni.
- macOS or Linux.
