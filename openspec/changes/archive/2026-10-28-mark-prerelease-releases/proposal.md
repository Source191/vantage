# Proposal

## Why

Vantage is ready for nuget.org (the Source191 organisation and the Trusted Publishing policy for `publish.yml` exist) but not yet for people to find and install it. A prerelease version keeps it out of nuget.org search and out of installs that name no version, and Vantage's own update check already ignores prereleases. Today `release.yml` publishes every version, a prerelease included, as an ordinary GitHub Release, so a preview would become the repository's "Latest" release ahead of the tested `v0.16.1`.

## What Changes

- `release.yml` marks the GitHub Release as a prerelease (`gh release create --prerelease`) when the new `Version` has a prerelease label, so `v0.16.1` stays "Latest" and the GitHub page says what the version is. A release without a label is published exactly as now.
- `Version` rises from `0.16.1` to `0.16.2-preview.1`, which `Assert-VersionBumped.ps1` already accepts (a prerelease of a higher patch ranks above `0.16.1`). The packaged code is the same as `v0.16.1`; main has nothing newer.
- The README's Releases and Publishing paragraphs say how a prerelease is released and published, and that an installed prerelease never checks for updates, so it is updated by hand with `dotnet tool update -g Source191.Vantage --prerelease` or a named `--version`.
- After the merge, `publish.yml` is run by hand for `v0.16.2-preview.1`; it needs no change, because its tag check already accepts a prerelease label.

Out of scope: any change to the tool's behaviour or the update check (a prerelease install still never checks, as `tool-updates` requires), publishing from `release.yml`, a prerelease channel or naming scheme beyond `-preview.N`, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Release tooling and a version number only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `.github\workflows\release.yml`: the publish step adds `--prerelease` for a version with a hyphen.
- `src\Source191.Vantage\Source191.Vantage.csproj`: `Version` `0.16.2-preview.1`.
- `README.md`: Releases and Publishing to nuget.org paragraphs.
- nuget.org: the first version of `Source191.Vantage`, a prerelease owned by Source191.
