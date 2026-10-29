# Tasks

## 1. Version without the commit

- [x] 1.1 Red: in `check.yml`, after "Install the packed tool", a step that runs the installed `vantage.exe --version` and fails unless its output is exactly the csproj `Version` (a check run packs as `Version-ci.<run>` with `--no-build`, so the assembly keeps `Version`); verify the same comparison fails locally against a Release build of the current code (it prints `+<commit>`)
- [x] 1.2 Green: `IncludeSourceRevisionInInformationalVersion` false in `Source191.Vantage.csproj`; verify the local comparison passes, `ToolVersionShould` still passes (the header keeps dropping build metadata from older builds), and `Version` rises to `0.17.0-preview.2`
- [x] 1.3 Verify `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `openspec validate version-without-commit` and `Assert-VersionBumped.ps1` pass

## Workflow follow-up

- Archive the change in the same pull request, push and open it; the `check` job's new step proves the packed tool.
