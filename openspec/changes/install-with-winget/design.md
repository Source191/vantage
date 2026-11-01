# Design

## Context

Vantage is packed as a framework-dependent dotnet tool (`PackAsTool`, net10.0). Four places assume that:

- `UpdateCheck` asks nuget.org's flat container for versions (`NuGetPackageVersions.ListAsync`) and reads the release notes from the version's nuspec (`NotesAsync`); `UpdateCheck.Source` is the nuget.org feed the update tab installs from.
- `TabScripts.Update` waits for Vantage to exit, runs `dotnet tool update -g Source191.Vantage --source ...` and starts the executable again.
- `check.yml` installs the packed nupkg with `dotnet tool install --tool-path` and smoke-tests that executable; `release.yml` attaches the nupkg to the GitHub Release; `publish.yml` pushes it to nuget.org by hand with Trusted Publishing; `tools\install-local.ps1` packs and installs it.
- The README's prerequisites list the .NET 10 SDK, Windows Terminal, PowerShell 7, git and Claude Code.

A measured self-contained single-file `win-x64` publish (1 November 2026, .NET 10.0.403) is a 38.6 MB `vantage.exe` plus `libonigwrap.dll` (Terminal.Gui's native dependency), 33.9 MB zipped, and `vantage --version` runs. The smoke check against it has not been run. winget's own `Anthropic.ClaudeCode` was 2.1.302 on the same day, below Vantage's 2.1.309 floor.

See proposal.md for why.

## Goals / Non-Goals

**Goals:**
- One install command and one upgrade command for a stable release, with no SDK.
- The update check never offers a version `winget upgrade` cannot yet install.
- Existing dotnet tool users have a one-key way across.
- The pull request check still smoke-tests the exact bytes that ship.

**Non-Goals:**
- Reducing the download size (no trimming, no AOT: Terminal.Gui and System.CommandLine use reflection the trimmer would need annotating for).
- Any change to what Vantage does with sessions, worktrees or tabs.
- Keeping two update paths for ever: the dotnet tool path exists only for the migration release.

## Decisions

### The build: self-contained, single file, not trimmed, x64 only
`PublishSingleFile`, `SelfContained`, `RuntimeIdentifier win-x64`, `EnableCompressionInSingleFile`, with `InvariantGlobalization` unchanged. The zip holds `vantage.exe`, `libonigwrap.dll`, `CHANGELOG.md`, `vantage.ico` and `THIRD-PARTY-NOTICES.txt`; the PDBs and XML documentation files the publish also writes are left out. A project's `.csproj` carries no comments, so the reasons go in `src\Source191.Vantage\README.md`.
- **Alternative: framework-dependent zip plus a .NET runtime dependency:** `Microsoft.DotNet.Runtime.10` is a winget package, so the zip would be about 1 MB. Rejected because winget would install a shared runtime the user did not ask for, and a runtime-only machine is a second prerequisite state to test.
- **Alternative: `win-arm64` too:** deferred; x64 runs under emulation, and a second runner and smoke run is a later change.
- **Alternative: native AOT:** rejected, see Non-Goals.

### The winget package: portable zip with three dependencies
`InstallerType: zip`, `NestedInstallerType: portable`, one nested `vantage.exe` with `PortableCommandAlias: vantage`, and `Dependencies.PackageDependencies` of `Microsoft.WindowsTerminal`, `Microsoft.PowerShell` and `Git.Git`. A portable package extracts to a per-user folder with a link on the PATH, so no administrator rights are needed, which matches how Vantage is used.
- **Dependencies are "install if missing":** the schema documents `PackageDependencies` as same-source package identifiers and documents no minimum version, so an old Windows Terminal that winget recognises may not be upgraded. Vantage's own minimum checks (Windows Terminal 1.25 or later, PowerShell 7, git) stay and say what is wrong.
- **Claude Code is deliberately not a dependency**, see proposal.md.

### The update source: winget-pkgs for versions, the tag's changelog for notes
The check lists the version folders of `manifests/s/Source191/Vantage` in `microsoft/winget-pkgs` through GitHub's contents API (anonymous, one request a day, well inside the unauthenticated limit of 60 an hour per address), and keeps the folder names that parse as stable versions (`ReleaseVersion.TryParse`). A folder exists only once the manifest's pull request is merged, so a version the check finds is one winget can install. The notes for a newer version come from `CHANGELOG.md` at the tag `v<version>` on `raw.githubusercontent.com`, through the existing `ReleaseNotes.TryParse`, so the sanitising rules in `security.md` are unchanged. A new adapter implements the existing `IPackageVersions` port (the nuget.org adapter goes), and `UpdateCheck`'s `PackageId` and `Source` constants change meaning with it.
- **Residual race:** winget's own index can trail the merged manifest by minutes. The update tab then shows `winget upgrade`'s output and stays open (a scenario in the spec); the next press works.
- **Alternative: read GitHub Releases:** announces a version hours or days before winget can install it. Rejected by the owner.
- **Alternative: run `winget upgrade --id ... --accept-source-agreements` in the background and parse it:** exact, but starts a process whose output is localised and unstable, which `security.md` already avoids for gh and az. Rejected.

### The update tab: `winget upgrade`, same shape as today
`TabScripts.Update` keeps its shape: `Wait-Process` on this Vantage, then `winget upgrade --id Source191.Vantage --exact --source winget`, then start Vantage only when `$LASTEXITCODE` is 0. Every value still goes through `PowerShellScript.Quote`; `winget` is found once by `ProgramLocator` like every program Vantage starts. A portable package keeps one install folder across versions, so the recorded executable path is still right after an upgrade (to be confirmed by the spike below).

### Telling the two installs apart: is the program a single-file bundle
A single-file build has no assembly location on disk; a dotnet tool does. One `InstallChannel` fact, read once in `SharedParts`, decides whether `U` runs the winget upgrade or the migration. Matching on the `~\.dotnet\tools\.store` path was the alternative; the bundle test does not depend on where a user's tools live. This is the only place the two are told apart, and it goes in the change that retires the dotnet tool.

### Migration: one last nuget.org release that carries the new code
The release that introduces winget is published to both: the zip to GitHub and winget, and, by hand through the existing `publish.yml`, the nupkg to nuget.org. Run as a dotnet tool it reads the winget-pkgs listing (so it offers the move only once winget has the same version), says in the header that Vantage is now installed with winget, and its `U` runs `winget install`, then `dotnet tool uninstall -g`, then starts the new executable. After it, the nuget.org package is deprecated with a pointer to winget (owner's step, in MAINTAINING). Removing the nupkg build and `publish.yml` is a separate change.
- **Alternative: leave dotnet tool users to find winget:** they would never see another release; the update notice is the only channel they read.

### Previews: the same zip, never winget
`release.yml` already publishes a GitHub prerelease for each merge that raises `Version`; it now attaches the zip. `check.yml` uploads the zip as the pull request's artefact. `tools\install-local.ps1` stops packing and installing a tool: it publishes the self-contained build to a fixed folder under `%LOCALAPPDATA%\Vantage\local`, prints the executable's full path and never touches the PATH, so it cannot replace a winget install; `-FromArtifact <folder>` extracts a check artefact's zip to the same place.
- **Assumption to confirm with the owner:** prereleases also keep their GitHub prerelease with the zip attached, in addition to the CI artefact (artefacts expire after 90 days). Drop the attachment if only CI artefacts are wanted.

### The release workflow: build once, test the zip that ships
`build-test-pack` publishes the self-contained build, zips it and outputs the zip's path; the nupkg is still packed beside it while migration is live. The `check` job's smoke step runs the headless smoke check against the unzipped `vantage.exe` instead of an installed tool, and the `--version` check reads it the same way. `release.yml` attests the zip (not only the nupkg) and attaches both. A final job, only for a stable version, runs `wingetcreate update Source191.Vantage --version <v> --urls <zip> --submit` with a repository secret holding a GitHub token that can open pull requests on a fork of `microsoft/winget-pkgs`. The first version is submitted by hand with `wingetcreate new`, which also creates the manifest the later runs update; nothing in the repository holds a manifest.

## Risks / Trade-offs

- **[The exe's path or base folder is wrong behind winget's link]** `TabScripts` is built from `Environment.ProcessPath`; `vantage.ico` and `CHANGELOG.md` are read from `AppContext.BaseDirectory`. → Spike task 1: install a local manifest, run it, print both, and move the two files into the bundle (embedded resources) if either lands in the links folder.
- **[A zip that winget extracts only in part]** The schema says one nested installer file is run; whether the other files in the archive are kept is not stated. → The same spike checks that `libonigwrap.dll` and the notes sit beside the executable after install; if not, `IncludeNativeLibrariesForSelfExtract` and an embedded changelog replace them.
- **[Unsigned executable]** SmartScreen or a managed machine's policy may block it. → Out of scope here; a signing decision follows if reports arrive.
- **[Download size]** about 34 MB against a small package. → Accepted; winget caches nothing but the download is once per upgrade.
- **[winget-pkgs review delay]** The first submission is reviewed by a person, later ones are validated automatically but can wait in a queue. → The update check follows what is merged, so users are never offered an unmerged version; the cost is a delay, not a failure.
- **[Rate limit]** The contents API allows 60 anonymous requests an hour per address. → One request a day; a failure is silent and retried at the next start, like every other check failure.
- **[Machines without winget]** Server and LTSC builds. → The GitHub Release zip is the documented fallback; `U` there only shows the notice and the release page.
- **[The locked executable]** `winget upgrade --all` fails while a Vantage runs, as `dotnet tool update` did. → Unchanged: the update tab waits first, and a failure leaves the tab open with winget's message.
- **[Two installs on one PATH during migration]** The shim in `~\.dotnet\tools` and winget's link may both exist until the tool is uninstalled. → The migration uninstalls the tool only after the winget install succeeds, and its tab starts the winget executable by full path.

## Migration Plan

1. Land this change (code, workflows, docs) with a minor version bump; a stable release is cut from `main`.
2. `release.yml` publishes the GitHub Release with the zip; submit the first manifest to winget-pkgs by hand; wait for the merge.
3. Run `publish.yml` on that tag, once, so dotnet tool users get the migrating build.
4. Deprecate the nuget.org package; point the README at winget alone.
5. Later, in its own change: remove `PackAsTool`, the nupkg packing, `publish.yml`, the migration code and `InstallChannel`.
- **Rollback:** a bad release is superseded by a new version in both places (nuget.org versions cannot be replaced, and a winget-pkgs version is removed only by a pull request); the previous winget version stays installable by `--version` until then.

## Open Questions

- Whether the manifest's `ReleaseNotesUrl` should point at the GitHub Release page or the changelog at the tag; either works without changing the specs.
- The zip's exact name (`vantage_<version>_win-x64.zip` is used in the spec as the example); only the first submission fixes it.
