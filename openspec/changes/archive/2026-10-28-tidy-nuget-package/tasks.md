# Tasks

## 1. Package contents

- [x] 1.1 Set `SatelliteResourceLanguages` to `en` in `Source191.Vantage.csproj`; verify a local `dotnet pack` holds no culture folders under `tools\net10.0\any`
- [x] 1.2 Set `PackageReleaseNotes` to the version's GitHub Release URL; verify the packed `.nuspec` has `<releaseNotes>` naming `releases/tag/v0.16.2-preview.2`
- [x] 1.3 Set `ContinuousIntegrationBuild` when `GITHUB_ACTIONS` is `true` in `Directory.Build.props`; verify a pack with `GITHUB_ACTIONS=true` leaves no `C:\Users` source path in `vantage.dll`'s PDB data while a plain local build still builds

## 2. README and version

- [x] 2.1 Replace the README's `<img>` with a Markdown image on `https://raw.githubusercontent.com/Source191/vantage/main/assets/icon/vantage-128.png`; verify the URL answers 200 (once merged) and the packed README has no `<img`
- [x] 2.2 Raise `Version` to `0.16.2-preview.2`; verify `Assert-VersionBumped.ps1 -Base origin/main -Head HEAD` exits 0 after the commit, and `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `openspec validate tidy-nuget-package` pass

## Workflow follow-up

- Archive the change in the same pull request, open it and merge it once `check` passes.
- Confirm `v0.16.2-preview.2` is a GitHub prerelease, run `publish.yml` for it, and once indexed unlist `0.16.2-preview.1` on nuget.org by hand.
