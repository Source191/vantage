# Proposal

## Why

0.17.0 went out as a full release when dialog-fixes merged, but the owner wants only previews until the security work and what follows it are done. Version cannot step back to a 0.17.0 preview: semantic versioning ranks every 0.17.0 preview below 0.17.0, so the version check would refuse it and an install of 0.17.0 would never be offered it.

## What Changes

- **Version:** `0.18.0-preview.1`, the first preview of the next minor; later pull requests raise the preview number as before.
- **GitHub:** the v0.17.0 GitHub Release is marked as a pre-release, so GitHub no longer shows it as the latest release. It was never published to nuget.org.
- No behaviour changes: `skip_specs: true`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

- `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).
- The v0.17.0 GitHub Release's pre-release flag.
