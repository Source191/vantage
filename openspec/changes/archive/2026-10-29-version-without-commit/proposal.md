# Proposal

## Why

`vantage --version` prints the package version followed by the commit it was built from (`0.16.2-preview.2+820cb62556c5b0f113f213aef4ed02e89704e118`), seen on a clean machine installed from nuget.org. The header already shows the version without it, and a person comparing their version with nuget.org or a GitHub Release expects the same text those show.

## What Changes

- The tool's informational version no longer carries the source revision, so `vantage --version` prints exactly the package version (`0.17.0-preview.2`, or `0.17.0-local.<timestamp>` for a local build).
- The `check` job's smoke step asserts that the installed tool's `--version` is exactly the packed version.
- `Version` rises to `0.17.0-preview.2`.

Out of scope: the header's version text (unchanged: it already drops build metadata and a local build's timestamp), the commit recorded in the package's repository metadata (unchanged), and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `command-line`: "The version is printed on request" prints the version without build metadata.

## Impact

- `src\Source191.Vantage\Source191.Vantage.csproj`: `IncludeSourceRevisionInInformationalVersion` false, `Version`.
- `.github\workflows\check.yml`: a version assertion on the installed tool.
- `openspec\specs\command-line\spec.md` through the delta.
