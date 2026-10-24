# Proposal

## Why

The .NET 10 SDK audits every package, direct and transitive, against nuget.org's vulnerability data on each restore, but `Directory.Build.props` keeps those advisories as warnings, so a package with a known vulnerability builds, merges and ships without anyone being stopped. Failing the build is a check that needs no external scanner.

## What Changes

- Remove `NU1901`, `NU1902`, `NU1903` and `NU1904` (low, moderate, high and critical advisories) from `WarningsNotAsErrors` in `Directory.Build.props`, so `TreatWarningsAsErrors` turns any known vulnerability into a restore error.
- `NU1900` (the vulnerability data could not be fetched) stays a warning, so a nuget.org outage does not fail a build.
- The rule applies everywhere the props file does: local builds, the `check` workflow (and so pull requests, through branch protection) and the `release` workflow, which both build through `build-test-pack`.

Out of scope: lock files (`packages.lock.json`), third-party scanners, a scheduled audit workflow, any change to package versions, multiple Claude accounts.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Build configuration only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Directory.Build.props`: one property and its comment.
- A newly published advisory against any package fails every build, local and CI, until the package is updated (`dotnet package update --vulnerable`) or the advisory is suppressed with `NuGetAuditSuppress`.
- No change to the tool, its keys, its state files or the `vantage here` command line.
