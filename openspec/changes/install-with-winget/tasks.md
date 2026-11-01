# Tasks

## 1. Spike: what winget does with the build

- [ ] 1.1 Publish the self-contained single-file build, write a local manifest (`winget settings --enable LocalManifestFiles`) for its zip with the portable alias `vantage`, run `winget install --manifest`, and record in `.claude\rules\windows.md` (dated, with the winget version): what `Environment.ProcessPath` and `AppContext.BaseDirectory` resolve to when run through the link, whether `libonigwrap.dll`, `CHANGELOG.md` and `vantage.ico` are extracted beside the executable, whether the install folder survives `winget upgrade`, and the exit code and message when the upgrade finds the executable locked. Verify by the recorded facts; if a path is wrong, add a task to group 2 to embed the file.
- [ ] 1.2 Run `dotnet run -- smoke <exe> Northwind.Smoke-smoke` from `tools\HeadlessTerminal` against the unzipped `vantage.exe` and verify exit 0.

## 2. The build

- [ ] 2.1 Add `PublishSingleFile`, `SelfContained`, `RuntimeIdentifier win-x64` and `EnableCompressionInSingleFile` to `Source191.Vantage.csproj` and a publish step that leaves out the PDBs and XML documentation files; write each setting's reason in `src\Source191.Vantage\README.md` (no comments in the csproj). Verify `dotnet publish` gives a folder holding `vantage.exe` and its native library and `vantage --version` prints the csproj `Version`.
- [ ] 2.2 Test then build the zip step in `build-test-pack`: a script test (or the check's existing format) asserting the zip holds `vantage.exe`, `libonigwrap.dll`, `CHANGELOG.md`, `vantage.ico` and `THIRD-PARTY-NOTICES.txt` and no `.pdb`. Verify the action outputs the zip path and the nupkg is still packed.

## 3. The update source

- [ ] 3.1 Domain: test `InstallChannel` (single-file bundle is winget, otherwise dotnet tool) with the fact passed in, then add it. Verify `InstallChannelShould`.
- [ ] 3.2 Port and adapter: write `WingetPackageVersionsShould` against a stub `HttpMessageHandler` (the folder listing, non-version folder names ignored, prerelease names ignored, failure, timeout and unreadable answers all `NothingLearned`; the changelog at the tag read as notes; a user agent of `Vantage/<version>`), then add `WingetPackageVersions` implementing `IPackageVersions`. Verify the new tests pass and no test reaches the network.
- [ ] 3.3 Replace `NuGetPackageVersions` in `ConsoleComposition` and delete it with `NuGetPackageVersionsShould`; change `UpdateCheck`'s constants and `UpdateCheckShould`'s fixtures. Verify `UpdateCheckShould` covers the "release not yet merged" and "notes cannot be read" scenarios.

## 4. The update and migration tabs

- [ ] 4.1 Test `TabScripts.Update` builds `winget upgrade --id Source191.Vantage --exact --source winget` after the `Wait-Process`, started again only on `$LASTEXITCODE` 0, with every value quoted; then change it. Verify `TabScriptsShould`.
- [ ] 4.2 Test a `TabScripts.Migrate` script (`winget install`, then `dotnet tool uninstall -g Source191.Vantage` only when the install exited 0, then the winget executable by full path, otherwise winget's output stays) and add it. Verify `TabScriptsShould` names the three outcomes (success, install fails, declined opens nothing).
- [ ] 4.3 Handler: test that the existing update handler chooses `Update` or `Migrate` from `InstallChannel` and returns its outcome record, then change it and its line in `Application\Effects\Composition.Handlers`. Verify `UpdateHandlerShould`, `EffectDispatcherShould` and `DialogEffectsShould` still pass.
- [ ] 4.4 Wording: add, with a fact and a `Wording*Should` test each, the header notice for a dotnet tool install and the migration dialogue text; change every sentence that names nuget.org (`WordingOfOutcomes`, `WordingOfConfig` rows and tooltips, `Wording` update-failed text, `Settings` and `SetupState` summaries). Verify by `grep nuget` over `src` finding only the migration release's own mentions.
- [ ] 4.5 Screen: the header shows the migration notice for a dotnet tool install and `U` opens What's new with Update now reading as the move. Verify the `Tui` and hand-off checks still pass and a screenshot test names the new text.

## 5. Workflows

- [ ] 5.1 `check.yml`: replace the nupkg install steps with unzipping the zip into a temp folder, then run the `--version` and smoke steps against that `vantage.exe`; upload the zip as the pull request's artefact. Verify a draft pull request's check passes with the zip.
- [ ] 5.2 `release.yml`: attest and attach the zip (and still the nupkg) for every release, stable or prerelease, and keep the stable release's notes and `--notes-start-tag`. Verify with a prerelease from a throwaway branch that the Release carries both files and `gh attestation verify` passes on the zip.
- [ ] 5.3 Add a stable-only job that runs `wingetcreate update` with the repository secret and a fork of winget-pkgs, and fails loudly when the pull request cannot be opened. Verify with `--what-if` style dry run on a test fork before the secret is real.
- [ ] 5.4 `Assert-ChangelogUpdated.ps1`: keep the 35,000-character limit only while the nupkg is still packed, and say so in its message. Verify its existing tests.

## 6. Local installs

- [ ] 6.1 `tools\install-local.ps1`: publish the self-contained build to `%LOCALAPPDATA%\Vantage\local`, print the executable's full path, never edit the PATH or touch a winget install; `-FromArtifact <folder>` extracts a check artefact's zip to the same place. Verify by running both forms and `vantage --version` from the printed path, with a winget-installed Vantage still first on the PATH.

## 7. Documentation

- [ ] 7.1 README: install section is `winget install Source191.Vantage` and Claude Code's native installer line; the prerequisites list loses the .NET SDK and shrinks to Claude Code; add the GitHub Release zip as the fallback and the preview instructions; update the Updates section. Verify every command runs as written on a clean machine or VM.
- [ ] 7.2 CONTRIBUTING (preview and local installs), MAINTAINING (the winget-pkgs token, the first submission, the migration release, deprecating the nuget.org package) and `.claude\rules\checks.md` and `windows.md` (the release, check and update-tab facts). Verify `openspec validate install-with-winget --strict` and a read-through for stale `dotnet tool` and nuget.org wording.
- [ ] 7.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` (minor, `0.y.z`) and add the `CHANGELOG.md` entries: Added winget, Changed update, Removed nuget.org installs. Verify `Assert-VersionBumped.ps1` and `Assert-ChangelogUpdated.ps1` pass.

## 8. Integration checks (owner, on real machines)

- [ ] 8.1 A clean Windows 11 VM with no .NET: `winget install Source191.Vantage`, `vantage` starts, Windows Terminal, PowerShell 7 and git were brought in, `vantage setup-terminal` works. Verify by the VM's `winget list`.
- [ ] 8.2 Upgrade from the previous winget version with `U`: the tab waits, upgrades, restarts, and background sessions are still listed. Verify on the status of the sessions before and after.
- [ ] 8.3 Migration: a machine with the previous dotnet tool presses `U`, ends with only the winget copy on the PATH. Verify `Get-Command vantage -All`.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs` describes the code on `main`.
- Propose a later change that retires the dotnet tool: `PackAsTool`, the nupkg, `publish.yml`, `InstallChannel` and the migration tab.
