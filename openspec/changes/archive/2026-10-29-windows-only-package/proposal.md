# Proposal

## Why

Vantage runs only on Windows, yet its package carries a native library for thirteen platforms. Terminal.Gui 2.5.0 depends on TextMateSharp (syntax highlighting in its Markdown view), which depends on Onigwrap, whose `libonigwrap` ships under `runtimes\<rid>\native` for Linux (six variants), macOS, Mac Catalyst and Windows. Vantage uses none of it. The ten non-Windows copies make up 2.6 MB of the 5.4 MB package and 6.9 MB of every install, and nuget.org's contents view suggests the tool supports platforms it does not.

## What Changes

- Packing drops every `runtimes\<rid>` folder whose RID is not a Windows one (`win-x64`, `win-arm64`, `win-x86` stay), so the package stays one framework-dependent tool with the same id, the same `vantage.exe` shim and the same install and update commands.
- The shared CI pack step fails when a package holds a `runtimes/` entry for any other platform, so a dependency update cannot bring them back unnoticed.
- `src\Source191.Vantage\README.md` records the setting and why; `Version` rises to `0.17.0-preview.4`.
- Winget support is recorded as a future enhancement: a GitHub issue describing a self-contained `vantage.exe` installed through `winget install`, with no .NET SDK, and a line in the README's "Roadmap and known limits" linking to it.

Out of scope: RID-specific tool packages (see design.md for why they were rejected), a self-contained or trimmed build, winget itself, refusing to install on Linux or macOS (the README already says Windows only), and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Nothing a user types or sees changes; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `src\Source191.Vantage\Source191.Vantage.csproj`: one target that filters the files published into the tool package, and the `Version` bump.
- `.github\actions\build-test-pack\action.yml`: the guard on the packed `.nupkg`, used by both `check.yml` and `release.yml`.
- `src\Source191.Vantage\README.md` and `README.md` (roadmap line).
- GitHub: one new issue on `Source191/vantage`.
- Installs and updates: unchanged commands; existing installs update in place.
