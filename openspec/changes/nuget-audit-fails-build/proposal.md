# Proposal

## Why

The .NET 10 SDK audits every package, direct and transitive, against nuget.org's vulnerability data on each restore, but `Directory.Build.props` keeps those advisories as warnings, so a package with a known vulnerability builds, merges and ships without anyone being stopped. Failing the build is a check that needs no external scanner.

## What Changes

- Remove the `WarningsNotAsErrors` exemption for `NU1900` to `NU1904` from `Directory.Build.props`, so `TreatWarningsAsErrors` turns any known vulnerability (`NU1901` to `NU1904`, low to critical) into a restore error.
- `NU1900` (the vulnerability data could not be fetched) also becomes an error, so a build never passes with an audit that did not run.
- The rule applies everywhere the props file does: local builds, the `check` workflow (and so pull requests, through branch protection) and the `release` workflow, which both build through `build-test-pack`.

Out of scope: lock files (`packages.lock.json`), third-party scanners, a scheduled audit workflow, any change to package versions, multiple Claude accounts.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Build configuration only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Directory.Build.props`: one property removed and its comment.
- A newly published advisory against any package fails every build, local and CI, until the package is updated (`dotnet package update --vulnerable`) or the advisory is suppressed with `NuGetAuditSuppress`.
- No change to the tool, its keys, its state files or the `vantage here` command line.
