# Tasks

## 1. Guard (red)

- [x] 1.1 In `.github\actions\build-test-pack\action.yml`'s "Read the version and find the package" step, open the `.nupkg` and throw, naming the entries, when any path contains `/runtimes/` not followed by `win`; verify by running the step's script locally against today's package (packed into a scratch folder) and seeing it fail with the ten non-Windows `libonigwrap` entries

## 2. Filter (green)

- [x] 2.1 Add the target to `src\Source191.Vantage\Source191.Vantage.csproj` that removes non-Windows `ResolvedFileToPublish` items by `RuntimeIdentifier` (design.md); verify that `dotnet build -c Release` then `dotnet pack src\Source191.Vantage -c Release --no-build` into a scratch folder gives one `.nupkg` holding only `runtimes/win-arm64`, `win-x64` and `win-x86`, that the 1.1 script passes on it, and record the new package size
  - Recorded 29 October 2026: the package is 3,512,666 bytes, down from 5,526,470, with only `runtimes/win-arm64`, `win-x64` and `win-x86` inside; the guard passes on it.
- [x] 2.2 Install that package with `dotnet tool install --tool-path` into a scratch folder and verify the folder has `vantage.exe` (not a `.cmd`), `vantage.exe --version` prints the packed version, and `dotnet run --project tools\HeadlessTerminal -- smoke <that exe> <an idle linked worktree>` exits 0
  - Recorded 29 October 2026: the tool path holds `vantage.exe` and no `.cmd`, `--version` prints `0.17.0-preview.3` (the version packed before 2.3's raise), the store holds only the three `win-*` runtimes, and the smoke check against a throwaway `Northwind.Smoke-smoke` worktree exits 0.
- [x] 2.3 Add the "Package settings" bullet to `src\Source191.Vantage\README.md` (Onigwrap, the RIDs kept, the rejected RID-specific route in a sentence) and raise `Version` to `0.17.0-preview.4`; verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `.github\scripts\Assert-VersionBumped.ps1` against `main` all pass

## 3. Winget on the roadmap

- [ ] 3.1 Open a GitHub issue on `Source191/vantage`, "Install with winget, without the .NET SDK", describing the self-contained `win-x64` and `win-arm64` zips on the GitHub Release, the portable manifest with the `vantage` alias and its PowerShell, Git and Windows Terminal dependencies, the hand-run submission to `microsoft/winget-pkgs`, `U` and the update check moving to winget, previews, signing, and the last nuget.org release moving installs across; verify `gh issue view` shows it
- [ ] 3.2 Add a line to the README's "Roadmap and known limits" linking to that issue; verify the link opens the issue and `openspec validate windows-only-package` passes

## Workflow follow-up

- Archive the change in the same pull request, open it and merge it once `check` passes; publish `v0.17.0-preview.4` to nuget.org with `publish.yml` after trying the release, and confirm nuget.info's contents view shows only `win-*` under `runtimes`.
