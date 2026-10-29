# Design

## Context

`PackAsTool` packs the publish output of a portable, framework-dependent build into `tools\net10.0\any\`, and that output copies every RID's native assets from the dependency graph. Onigwrap 1.0.11 (Terminal.Gui 2.5.0, through TextMateSharp) is the only package with any: thirteen `runtimes\<rid>\native\libonigwrap.*`. Each reaches the publish list as a `ResolvedFileToPublish` item with `AssetType` `native`, `RuntimeIdentifier` set to the RID and `RelativePath` `runtimes\<rid>\native\<file>` (checked with `dotnet msbuild -getItem:ResolvedFileToPublish -t:ComputeResolvedFilesToPublishList`).

Today's package is 5.4 MB, with 2.6 MB of `runtimes` entries (6.9 MB once installed). The installed `~\.dotnet\tools\vantage.exe` is a stable apphost shim, so `Environment.ProcessPath`, which `Program.cs` hands to `TabScripts` (the `U` relaunch and `claim-tab`) and to `TerminalSetupRunner` (the "Vantage" profile's command line), does not change between versions.

## Goals / Non-Goals

**Goals:**
- One package, the same id and the same install and update experience, with only Windows native assets inside.
- A failing CI step, not a silent regression, if a later dependency brings other platforms back.

**Non-Goals:**
- Removing Onigwrap or its Windows binaries: Vantage never calls it, but whether Terminal.Gui loads it is Terminal.Gui's business, and 0.6 MB is not worth the risk.
- Changing what `dotnet build` copies to `bin` for development; only the packed tool matters.

## Decisions

### Filter the publish list by RID, in the tool project

A target in `Source191.Vantage.csproj`, after `ComputeResolvedFilesToPublishList`, removes each `ResolvedFileToPublish` whose `RuntimeIdentifier` is set and does not start with `win`. The pack-as-tool step publishes from that list, so the files never reach the package. Keying on the item's `RuntimeIdentifier` metadata rather than on paths keeps it independent of which package the asset came from and of path separators. All `win-*` stay, `win-x86` included, so any Windows host that can run .NET 10 finds its binary.

If the filtered list does not reach the package (the tool pack gathers files through a different item), the target hooks before the step that gathers them instead; the guard below tells which.

*Alternatives:*

- **RID-specific tool packages** (`ToolPackageRuntimeIdentifiers` `win-x64;win-arm64` with `SelfContained` false, the .NET 10 feature). Tried on 29 October 2026 with SDK 10.0.403 and rejected:
  - It installs a `vantage.cmd` batch shim, not `vantage.exe`. The smoke check runs `<tool-path>\vantage.exe`, `cmd.exe` joins the process chain, and Ctrl+C in a batch file asks "Terminate batch job (Y/N)?".
  - `Environment.ProcessPath` becomes the versioned `.store\source191.vantage\<version>\...\vantage.exe`. `dotnet tool update` deletes that folder, so the `U` tab's relaunch fails, and the "Vantage" profile written by `setup-terminal` points at a missing file after every update.
  - `dotnet pack --no-build`, which `build-test-pack` uses, packs whatever per-RID builds happen to be in `bin` (it fails from a clean build), because `dotnet build` builds only the portable output.
  - It produces three packages (a pointer and one per RID) with two new permanent nuget.org ids, and `build-test-pack`, `release.yml`, `publish.yml` (RID packages before the pointer) and `install-local.ps1 -FromArtifact` all assume one.
- **`ExcludeAssets="native"` on a direct Onigwrap reference.** It removes the Windows binaries too; see Non-Goals.
- **Leave it.** It costs every user 5.3 MB of disc for nothing, and nuget.org's contents view keeps implying the tool runs on Linux and macOS.

### Guard the packed package in CI

The "Read the version and find the package" step of `.github\actions\build-test-pack\action.yml` opens the `.nupkg` (`System.IO.Compression.ZipFile`) and throws, naming the entries, if any path contains `/runtimes/` without continuing as `/runtimes/win`. Both `check.yml` and `release.yml` use the action, so neither a pull request nor a release can ship them. This is the test of the MSBuild target: an xunit fact would have to pack the tool and is slower and further from what ships.

### Where the reason lives

The csproj carries no comments; `src\Source191.Vantage\README.md`'s "Package settings" gets one bullet naming Onigwrap, the RIDs kept and the rejected RID-specific route, in one or two sentences.

## Risks / Trade-offs

- [A Terminal.Gui update adds a different native dependency] → The target filters by RID for every package, not by Onigwrap, and the guard fails the build if anything slips through.
- [Linux or macOS users can still install the package and get a failure at run time] → Unchanged from today; the README says Windows only. Refusing the install needs RID-specific packages, rejected above.
- [The target misses a publish path, e.g. a future single-file or self-contained pack] → The guard checks the package, whatever produced it.

## Migration Plan

None. The package id, shim and commands are unchanged, so `dotnet tool update -g Source191.Vantage` (or `U`) moves an install to `0.17.0-preview.4` in place. Rollback is reverting the target; nothing outside the package changes.
