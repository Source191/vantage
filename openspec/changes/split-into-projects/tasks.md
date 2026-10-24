# Tasks

## 1. Package management

- [ ] 1.1 Add `Directory.Packages.props` with every current package version and `ManagePackageVersionsCentrally`; strip versions from the csproj files; verify `dotnet build -c Debug` and `dotnet test` pass unchanged

## 2. Tui project

- [ ] 2.1 Create `src\Source191.Vantage.Tui` (class library, Terminal.Gui, reference to the tool project for now) and `git mv` the `Tui` folder into it; verify build, tests and `dotnet format --verify-no-changes` pass
- [ ] 2.2 Verify with `dotnet run --project tools\HeadlessTerminal -- check` that the screens still run

## 3. Core project

- [ ] 3.1 Create `src\Source191.Vantage.Core` (class library, no packages) and `git mv` `Domain`, `Ports` and `Application` into it; point `Tui` at `Core` instead of the tool project; verify build and tests pass
- [ ] 3.2 Verify `Core.csproj` has no `PackageReference` and that the architecture test still passes

## 4. Infrastructure project

- [ ] 4.1 Create `src\Source191.Vantage.Infrastructure` (class library, FileSystemGlobbing, Logging.Abstractions, reference to `Core`) and `git mv` `Adapters` into it; add `InternalsVisibleTo` for its test project; the exe references all three; verify build and tests pass
- [ ] 4.2 Run `tools\install-local.ps1`, start `vantage` in a fresh tab and open a session; verify the tool runs from the installed package with every assembly present

## 5. Test projects

- [ ] 5.1 Create `test\Source191.Vantage.Core.Tests` and move the pure tests and `TestDoubles` fakes listed in design.md; verify `dotnet test test\Source191.Vantage.Core.Tests` passes and the project has no reference to `Infrastructure` or `HeadlessTerminal`
- [ ] 5.2 Rename the remaining test project `Source191.Vantage.Infrastructure.Tests`, keep the integration tests and `TemporaryGitRoot`, and verify `dotnet test` passes
- [ ] 5.3 Create `tools\HeadlessTerminal.Tests` and move the five `HeadlessTerminal\*Should.cs` tests; switch the tool's `InternalsVisibleTo`; verify `dotnet test` at the solution passes with all three projects
- [ ] 5.4 Verify `dotnet test test\Source191.Vantage.Core.Tests` completes with no temp folders created (watch `%TEMP%` for `vantage-*` folders)

## 6. Pipeline and documentation

- [ ] 6.1 Update `.github\workflows\check.yml` and `.github\actions\build-test-pack\action.yml` to test at the solution and pack `src\Source191.Vantage`; verify a pull-request run is green
- [ ] 6.2 Update `tools\install-local.ps1`, CLAUDE.md (test loop, Stryker invocation, project map) and the README developer and CI sections; verify every command in them runs as written
- [ ] 6.3 Update `openspec\config.yaml` context with the project names in one sentence; verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 7: the whole change, one commit per task group, each a `git mv` plus csproj edits. A half-split solution is worse than either state. Bump `Version` to 0.3.1 so a release proves the tool package installs with four assemblies. Archive the change in this PR.
- Nothing else may run alongside this PR: every later change touches files it moves.
