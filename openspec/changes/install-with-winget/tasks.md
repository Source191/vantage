# Tasks

## 1. Spike: what winget does with the build

- [x] 1.1 Publish the self-contained single-file build, write a local manifest (`winget settings --enable LocalManifestFiles`) for its zip with the portable alias `torni`, run `winget install --manifest`, and record in `.claude\rules\windows.md` (dated, with the winget version): what `Environment.ProcessPath` and `AppContext.BaseDirectory` resolve to when run through the link, whether `libonigwrap.dll`, `CHANGELOG.md` and `torni.ico` are extracted beside the executable, whether the install folder survives `winget upgrade`, and the exit code and message when the upgrade finds the executable locked. Verify by the recorded facts; if a path is wrong, add a task to group 2 to embed the file.
- [x] 1.2 Run `dotnet run -- smoke <exe> Northwind.Smoke-smoke` from `tools\HeadlessTerminal` against the unzipped `torni.exe` and verify exit 0.

## 2. The build

- [x] 2.1 Add `PublishSingleFile`, `SelfContained`, `RuntimeIdentifier win-x64` and `EnableCompressionInSingleFile` in a publish profile (`Properties\PublishProfiles\win-x64.pubxml`) that leaves out the PDBs and XML documentation files; write each setting's reason in `src\Source191.Torni\README.md` (no comments in the csproj). Verify `dotnet publish` gives a folder holding `torni.exe` and its native library and `torni --version` prints the csproj `Version`.
- [x] 2.2 Test then build the zip step in `build-test-pack`: a script test (or the check's existing format) asserting the zip holds `torni.exe`, `libonigwrap.dll`, `CHANGELOG.md`, `torni.ico` and `THIRD-PARTY-NOTICES.txt` and no `.pdb`. Verify the action outputs the zip path.
- [x] 2.3 Remove the dotnet tool: `PackAsTool` and the package properties and items from `Source191.Torni.csproj`, the `RemoveNonWindowsNativeAssets` target, `icon.png` and `IconAssetsShould`, the pack step in `build-test-pack` and `publish.yml`. Verify the publish folder still holds exactly the five files and the build passes.

## 3. The update source

- [x] 3.1 Port and adapter: write `WingetPackageVersionsShould` against a stub `HttpMessageHandler` (the folder listing, non-version folder names ignored, prerelease names ignored, failure, timeout and unreadable answers all `NothingLearned`; the changelog at the tag read as notes; a user agent of `Torni/<version>`), then add `WingetPackageVersions` implementing `IPackageVersions`. Verify the new tests pass and no test reaches the network.
- [x] 3.2 Replace `NuGetPackageVersions` in `ConsoleComposition` and delete it with `NuGetPackageVersionsShould`; change `UpdateCheck`'s constants and `UpdateCheckShould`'s fixtures. Verify `UpdateCheckShould` covers the "release not yet merged" and "notes cannot be read" scenarios.

## 4. The update tab

- [x] 4.1 Test `TabScripts.Update` builds `winget upgrade --id Source191.Torni --exact --source winget` after the `Wait-Process`, started again only on `$LASTEXITCODE` 0, with every value quoted; then change it. Verify `TabScriptsShould` and `PowerShellQuotingShould`.
- [x] 4.2 Handler: test that the update handler opens the tab with winget's full path and, when winget is not on the PATH, opens nothing and returns `WingetMissingOutcome`; only a tab that opened makes the screen quit. Verify `StartUpdateHandlerShould`, `EffectDispatcherShould` and `DialogEffectsShould`.
- [x] 4.3 Wording: change every sentence that names nuget.org (`WordingOfOutcomes`, `WordingOfConfig` rows and tooltips, `Wording` update-failed text, `Settings` and `SetupState` summaries) and add the missing-winget outcome, each with a `Wording*Should` fact. Verify by `grep nuget` over `src` finding none.
- [ ] 4.4 Screen: `U` opens What's new and Update now opens the winget tab. Verify the hand-off check and `smoke-update` (owner, with seeded state) still pass.

## 5. Workflows

- [x] 5.1 `pipeline.yml`: replace the nupkg install steps with unzipping the zip into a temp folder, then run the `--version` and smoke steps against that `torni.exe`; upload the zip as the pull request's artefact. Verify a draft pull request's check passes with the zip.
- [ ] 5.2 `pipeline.yml`'s release job: attest and attach the zip for every release, stable or prerelease, and keep the stable release's notes and `--notes-start-tag`. Verify with the first prerelease from `main` that the Release carries the zip and `gh attestation verify` passes on it.
- [ ] 5.3 Rewrite `publish.yml` for winget: run by hand with a stable tag, in the `winget` environment (owner approval, `main` only, `WINGET_PKGS_TOKEN` as its secret); refuse a prerelease tag, verify the zip's attestation from `pipeline.yml` on `main`, fail while winget-pkgs has no Source191.Torni, run `wingetcreate update --submit`, and fail loudly when the pull request cannot be opened. Verify with a dry run on a test fork before the secret is real.
- [x] 5.4 `Assert-ChangelogUpdated.ps1`: drop the 35,000-character limit, which only nuget.org's release notes needed. Verify it passes between `main` and the branch.

## 6. Local installs

- [x] 6.1 `tools\install-local.ps1`: publish the self-contained build to `%LOCALAPPDATA%\Torni\local`, print the executable's full path, never edit the PATH or touch a winget install; `-FromArtifact <folder>` extracts a check artefact's zip to the same place. Verify by running both forms and `torni --version` from the printed path, with the installed Torni still first on the PATH.

## 7. Documentation

- [ ] 7.1 README: install section is `winget install Source191.Torni` and Claude Code's native installer line; the prerequisites list loses the .NET SDK and shrinks to Claude Code; add the GitHub Release zip as the fallback and the preview instructions. Verify every command runs as written on a clean machine or VM.
- [x] 7.2 CONTRIBUTING (preview and local installs), MAINTAINING (the winget-pkgs token and the first submission) and `.claude\rules\checks.md` and `windows.md` (the release, check and update-tab facts). Verify `openspec validate install-with-winget --strict` and a read-through for stale `dotnet tool` and nuget.org wording.
- [x] 7.3 Raise `Version` in `src\Source191.Torni\Source191.Torni.csproj` and add the `CHANGELOG.md` entries: Added winget, Changed update, Removed nuget.org installs. Verify `Assert-VersionBumped.ps1` and `Assert-ChangelogUpdated.ps1` pass.

## 8. Integration checks (owner, on real machines)

- [ ] 8.1 A clean Windows 11 VM with no .NET: `winget install Source191.Torni`, `torni` starts, Windows Terminal, PowerShell 7 and git were brought in, `torni setup-terminal` works. Verify by the VM's `winget list`.
- [ ] 8.2 Upgrade from the previous winget version with `U`: the tab waits, upgrades, restarts, and background sessions are still listed. Verify on the status of the sessions before and after.
- [ ] 8.3 Retire nuget.org: unlist and deprecate every `Source191.Vantage` version (the package keeps the name it was published under), and remove the Trusted Publishing policy, the `NUGET_USER` secret and the `nuget` environment, as the pull request's description lists. Verify the package page shows no listed version.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs` describes the code on `main`; at archive, also change the `tool-updates` spec's Purpose, which still names nuget.org.