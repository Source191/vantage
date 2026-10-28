# Proposal

## Why

The first nuget.org package, `0.16.2-preview.1`, shows four flaws on nuget.org and in package explorers: it carries translated resource folders (cs, de, fr-FR, ja-JP and others) from System.CommandLine and Terminal.Gui although Vantage is English only; its README's icon is an HTML `<img>` with a relative path, which nuget.org shows as raw text; it has no release notes; and its assemblies are reported as non-deterministic because CI builds keep local source paths.

## What Changes

- `Source191.Vantage.csproj` sets `SatelliteResourceLanguages` to `en`, so no satellite resource assemblies are built or packed and the libraries' own messages stay English on every Windows display language.
- `Source191.Vantage.csproj` sets `PackageReleaseNotes` to a line pointing at the version's GitHub Release (`releases/tag/v$(Version)`), where `release.yml` already generates the notes; no changelog file is added.
- The README's icon becomes a Markdown image with an absolute `raw.githubusercontent.com` URL, which both GitHub and nuget.org render (shown at 128 px rather than 96).
- `Directory.Build.props` sets `ContinuousIntegrationBuild` when `GITHUB_ACTIONS` is `true`, so CI builds map source paths and are reported as deterministic; local builds are unchanged.
- `Version` rises to `0.16.2-preview.2`, published to nuget.org as a prerelease the same way as `0.16.2-preview.1`.

Out of scope: any change to the tool's behaviour, a CHANGELOG file, localising Vantage, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Packaging and documentation only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `src\Source191.Vantage\Source191.Vantage.csproj`: `SatelliteResourceLanguages`, `PackageReleaseNotes`, `Version`.
- `Directory.Build.props`: `ContinuousIntegrationBuild` under GitHub Actions.
- `README.md`: the icon line.
- nuget.org: `0.16.2-preview.2`, and `0.16.2-preview.1` unlisted by hand once it is replaced.
