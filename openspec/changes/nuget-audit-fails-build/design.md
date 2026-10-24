# Design

## Context

`Directory.Build.props` sets `TreatWarningsAsErrors` and exempts `NU1900` to `NU1904`. Every project targets `net10.0`, so `NuGetAuditMode` already defaults to `all` and transitive packages are audited. `check.yml` and `release.yml` both build through `.github\actions\build-test-pack`, whose `dotnet build` runs the restore.

## Goals / Non-Goals

**Goals:**
- Any known vulnerability, at any severity, fails the restore and therefore the build.

**Non-Goals:**
- A CI-only rule; the same rule applies on a developer machine so a failure is seen before a push.

## Decisions

- **Promote the audit warnings rather than parse `dotnet package list --vulnerable`:** the list command exits 0 even when it finds vulnerabilities, so a workflow step would have to parse its JSON. Removing the exemption reuses the restore that already runs and covers both workflows with one line.
- **Every severity, not only high and critical:** the user chose to fail on any vulnerability. `NuGetAuditLevel` is left at its default of `low`.
- **Keep `NU1900` exempt:** it reports that the audit could not run, not that a package is vulnerable; failing on it would tie every build to nuget.org's availability.

## Risks / Trade-offs

- [A new advisory blocks unrelated pull requests and releases] → update the package with `dotnet package update --vulnerable` (or find the parent of a transitive package with `dotnet nuget why`), or record an accepted advisory with `<NuGetAuditSuppress Include="<advisory URL>" />`.
- [`NU1900` hides an audit that never ran] → it still appears as a warning in the build log.

## Migration Plan

Merge the change; `dotnet package list --vulnerable --include-transitive` reports no vulnerable packages on `main` today, so nothing fails at once. Rollback is restoring the removed codes.
