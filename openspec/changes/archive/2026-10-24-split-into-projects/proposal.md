# Proposal

## Why

With the namespaces straightened by `untangle-layers`, the compiler can enforce the layering that today only an architecture test guards. Separate projects also give a fast, pure test project that needs no git, no processes and no temp folders, and stop the application test project from referencing the HeadlessTerminal tool.

## What Changes

- Four source projects: `Source191.Vantage.Core` (Domain, Ports, Application; no package references), `Source191.Vantage.Infrastructure` (every adapter, including the UI Automation tab switching and its Interop.UIAutomationClient package), `Source191.Vantage.Tui` (Terminal.Gui screens and dialogs), and the existing `Source191.Vantage` executable reduced to `Program.cs` and the command router, still packed as the `vantage` tool.
- Three test projects: `Source191.Vantage.Core.Tests` (pure), `Source191.Vantage.Infrastructure.Tests` (real git, processes and files), `HeadlessTerminal.Tests` beside the tool.
- Central package versions in `Directory.Packages.props`; `InternalsVisibleTo` per project.
- CI, the local install script, CLAUDE.md and the README follow the new paths.

Out of scope: any code change beyond moving files and fixing `using` lines, the generic screen, effect handlers, value objects, multiple Claude accounts. Depends on `untangle-layers` being archived first.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Structure only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Source191.Vantage.slnx`, four csproj files under `src\`, three under `test\` and `tools\`, `Directory.Build.props`, new `Directory.Packages.props`.
- `.github\workflows\check.yml`, `.github\actions\build-test-pack\action.yml`, `tools\install-local.ps1`, CLAUDE.md test commands, README developer and CI sections, the Stryker invocation in CLAUDE.md.
- The published tool package is unchanged for users: one nupkg, same command name.
