# Design

## Context

Torni is packed as a framework-dependent dotnet tool (`PackAsTool`, net10.0). Four places assume that:

- `UpdateCheck` asks nuget.org's flat container for versions (`NuGetPackageVersions.ListAsync`) and reads the release notes from the version's nuspec (`NotesAsync`); `UpdateCheck.Source` is the nuget.org feed the update tab installs from.
- `TabScripts.Update` waits for Torni to exit, runs `dotnet tool update -g Source191.Torni --source ...` and starts the executable again.
- `pipeline.yml` installs the packed nupkg with `dotnet tool install --tool-path` and smoke-tests that executable; `pipeline.yml`'s release job attaches the nupkg to the GitHub Release; `publish.yml` pushes it to nuget.org by hand with Trusted Publishing; `tools\install-local.ps1` packs and installs it.
- The README's prerequisites list the .NET 10 SDK, Windows Terminal, PowerShell 7, git and Claude Code.

A measured self-contained single-file `win-x64` publish (1 November 2026, .NET 10.0.403) is a 38.6 MB `torni.exe` plus `libonigwrap.dll` (Terminal.Gui's native dependency), 33.9 MB zipped, and `torni --version` runs. The smoke check against it has not been run. winget's own `Anthropic.ClaudeCode` was 2.1.302 on the same day, below Torni's 2.1.309 floor.

See proposal.md for why.

## Goals / Non-Goals

**Goals:**
- One install command and one upgrade command for a stable release, with no SDK.
- The update check never offers a version `winget upgrade` cannot yet install.
- The pull request check still smoke-tests the exact bytes that ship.

**Non-Goals:**
- Reducing the download size (no trimming, no AOT: Terminal.Gui and System.CommandLine use reflection the trimmer would need annotating for).
- Any change to what Torni does with sessions, worktrees or tabs.
- Moving existing dotnet tool installs across: nuget.org has only previews, which never check for updates.

## Decisions

### The build: self-contained, single file, not trimmed, x64 only
`PublishSingleFile`, `SelfContained`, `RuntimeIdentifier win-x64`, `EnableCompressionInSingleFile`, with `InvariantGlobalization` unchanged. The zip holds `torni.exe`, `libonigwrap.dll`, `CHANGELOG.md`, `torni.ico` and `THIRD-PARTY-NOTICES.txt`; the PDBs and XML documentation files the publish also writes are left out. The settings live in a publish profile, `Properties\PublishProfiles\win-x64.pubxml`, rather than the `.csproj`, so `dotnet build` keeps its output folder (the hand-off check runs `bin\Release\net10.0\torni.exe`) and the test projects can still reference a framework-dependent program. A project's `.csproj` carries no comments, so the reasons go in `src\Source191.Torni\README.md`.
- **Alternative: framework-dependent zip plus a .NET runtime dependency:** `Microsoft.DotNet.Runtime.10` is a winget package, so the zip would be about 1 MB. Rejected because winget would install a shared runtime the user did not ask for, and a runtime-only machine is a second prerequisite state to test.
- **Alternative: `win-arm64` too:** deferred; x64 runs under emulation, and a second runner and smoke run is a later change.
- **Alternative: native AOT:** rejected, see Non-Goals.

### The winget package: portable zip with three dependencies
`InstallerType: zip`, `NestedInstallerType: portable`, one nested `torni.exe` with `PortableCommandAlias: torni`, and `Dependencies.PackageDependencies` of `Microsoft.WindowsTerminal`, `Microsoft.PowerShell` and `Git.Git`. A portable package extracts to a per-user folder with a link on the PATH, so no administrator rights are needed, which matches how Torni is used.
- **Dependencies are "install if missing":** the schema documents `PackageDependencies` as same-source package identifiers and documents no minimum version, so an old Windows Terminal that winget recognises may not be upgraded. Torni's own minimum checks (Windows Terminal 1.25 or later, PowerShell 7, git) stay and say what is wrong.
- **Claude Code is deliberately not a dependency**, see proposal.md.

### The update source: winget-pkgs for versions, the tag's changelog for notes
The check lists the version folders of `manifests/s/Source191/Torni` in `microsoft/winget-pkgs` through GitHub's contents API (anonymous, one request a day, well inside the unauthenticated limit of 60 an hour per address), and keeps the folder names that parse as stable versions (`ReleaseVersion.TryParse`). A folder exists only once the manifest's pull request is merged, so a version the check finds is one winget can install. The notes for a newer version come from `CHANGELOG.md` at the tag `v<version>` on `raw.githubusercontent.com`, through the existing `ReleaseNotes.TryParse`, so the sanitising rules in `security.md` are unchanged. A new adapter implements the existing `IPackageVersions` port (the nuget.org adapter goes), and `UpdateCheck`'s `PackageId` and `Source` constants change meaning with it.
- **Residual race:** winget's own index can trail the merged manifest by minutes. The update tab then shows `winget upgrade`'s output and stays open (a scenario in the spec); the next press works.
- **Alternative: read GitHub Releases:** announces a version hours or days before winget can install it. Rejected by the owner.
- **Alternative: run `winget upgrade --id ... --accept-source-agreements` in the background and parse it:** exact, but starts a process whose output is localised and unstable, which `security.md` already avoids for gh and az. Rejected.

### The update tab: `winget upgrade`, same shape as today
`TabScripts.Update` keeps its shape: `Wait-Process` on this Torni, then `winget upgrade --id Source191.Torni --exact --source winget`, then start Torni only when `$LASTEXITCODE` is 0. Every value still goes through `PowerShellScript.Quote`; `winget` is found once by `ProgramLocator` like every program Torni starts. A portable package keeps one install folder across versions, so the recorded executable path is still right after an upgrade (to be confirmed by the spike below).

### No migration from the dotnet tool
The nupkg, `PackAsTool`, `publish.yml`'s nuget.org push and the package icon are removed in this change, and nothing in Torni tells a dotnet tool install apart or moves it. nuget.org has only previews of it, as `Source191.Vantage` (0.16.2-preview.1 to 0.18.0-preview.3, 247 downloads in all on 3 November 2026), and a prerelease build never checks for updates, so a migration release could reach no installed copy. The owner unlists and deprecates the nuget.org package by hand (nuget.org never deletes a package on request) and removes the Trusted Publishing policy, the `NUGET_USER` secret and the `nuget` environment.
- **Alternative: one last nuget.org release that moves dotnet tool users:** designed first (an install-channel fact, a move tab running `winget install` then `dotnet tool uninstall`), and dropped by the owner for a simpler repository once it was clear it would reach nobody.
### Previews: the same zip, never winget
`pipeline.yml`'s release job already publishes a GitHub prerelease for each merge that raises `Version`; it now attaches the zip. `pipeline.yml` uploads the zip as the pull request's artefact. `tools\install-local.ps1` stops packing and installing a tool: it publishes the self-contained build to a fixed folder under `%LOCALAPPDATA%\Torni\local`, prints the executable's full path and never touches the PATH, so it cannot replace a winget install; `-FromArtifact <folder>` extracts a check artefact's zip to the same place.
- **Assumption to confirm with the owner:** prereleases also keep their GitHub prerelease with the zip attached, in addition to the CI artefact (artefacts expire after 90 days). Drop the attachment if only CI artefacts are wanted.

### The release workflow: build once, test the zip that ships
`build-test-pack` publishes the self-contained build, zips it and outputs the zip's path; it packs no nupkg. The `check` job's smoke step runs the headless smoke check against the unzipped `torni.exe` instead of an installed tool, and the `--version` check reads it the same way. `pipeline.yml`'s release job attests and attaches the zip. `publish.yml` keeps its place as the hand-run gate between a release and users, now for winget: the owner tries a stable release's zip, then runs it with the release's tag. It refuses a prerelease tag, verifies the zip's attestation was signed by `pipeline.yml` from `main`, and runs `wingetcreate update Source191.Torni --version <v> --urls <zip> --submit` with a GitHub token that can open pull requests on a fork of `microsoft/winget-pkgs`. The token is a secret of a `winget` environment whose required reviewer is the owner and which accepts only `main`, so only an approved run from `main` can read it. A separate hand-run workflow was chosen over an approval-gated job in `pipeline.yml` because it reads more plainly and can submit any release at any time; the attestation check makes up for the tag being typed. The first version is submitted by hand with `wingetcreate new`, which also creates the manifest the later runs update; nothing in the repository holds a manifest. Until that manifest is merged, `publish.yml` finds no `manifests/s/Source191/Torni` in winget-pkgs and fails saying so.

## Risks / Trade-offs

- **[The exe's path or base folder is wrong behind winget's link]** `TabScripts` is built from `Environment.ProcessPath`; `torni.ico` and `CHANGELOG.md` are read from `AppContext.BaseDirectory`. → Spike task 1 (3 November 2026): through the link both are the links folder, so `ProgramFolder.Of` follows the link to the package folder, where the files are; no embedding is needed.
- **[A zip that winget extracts only in part]** The schema says one nested installer file is run; whether the other files in the archive are kept is not stated. → The spike found every file of the zip beside the executable after install.
- **[Unsigned executable]** SmartScreen or a managed machine's policy may block it. → Out of scope here; a signing decision follows if reports arrive.
- **[Download size]** about 34 MB against a small package. → Accepted; winget caches nothing but the download is once per upgrade.
- **[winget-pkgs review delay]** The first submission is reviewed by a person, later ones are validated automatically but can wait in a queue. → The update check follows what is merged, so users are never offered an unmerged version; the cost is a delay, not a failure.
- **[Rate limit]** The contents API allows 60 anonymous requests an hour per address. → One request a day; a failure is silent and retried at the next start, like every other check failure.
- **[Machines without winget]** Server and LTSC builds. → The GitHub Release zip is the documented fallback; `U` there only shows the notice and the release page.
- **[The locked executable]** `winget upgrade --all` fails while a Torni runs, as `dotnet tool update` did. → Unchanged: the update tab waits first, and a failure leaves the tab open with winget's message.
- **[A dotnet tool preview stays installed]** Someone who installed a preview from nuget.org keeps it, and it never offers an update. → Accepted: they are a handful of testers; the deprecation message on nuget.org names winget.

## Migration Plan

1. Land this change (code, workflows, docs); a stable release is cut from `main` when the owner chooses.
2. `pipeline.yml`'s release job publishes the GitHub Release with the zip; submit the first manifest to winget-pkgs by hand; wait for the merge. Later stable releases are submitted by running `publish.yml`.
3. Unlist and deprecate every nuget.org version, and remove the nuget.org publishing setup (a one-off owner step, listed in the pull request rather than MAINTAINING.md).
- **Rollback:** a bad release is superseded by a new version (a winget-pkgs version is removed only by a pull request); the previous winget version stays installable by `--version` until then.
## Open Questions

- Whether the manifest's `ReleaseNotesUrl` should point at the GitHub Release page or the changelog at the tag; either works without changing the specs.
- The zip's exact name (`torni_<version>_win-x64.zip` is used in the spec as the example); only the first submission fixes it.
