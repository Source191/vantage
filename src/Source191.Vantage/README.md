# Source191.Vantage

The packed tool: `Program.cs` and the package settings. The user guide is the repository's [README](../../README.md), which is also the package readme on nuget.org.

## Package settings

- **`SatelliteResourceLanguages` `en`:** Vantage is English only, so the translated resources of System.CommandLine and Terminal.Gui are neither built nor packed, and those libraries' own messages stay English on every Windows display language.
- **`IncludeSourceRevisionInInformationalVersion` false:** `vantage --version` prints exactly the package version that nuget.org and the GitHub Release show, with no `+<commit>`. The commit is still recorded in the package's repository metadata. The `check` job asserts this on the installed tool.
- **`PackageReleaseNotes`:** a link to the version's GitHub Release, where `release.yml` generates the notes; there is no changelog file to keep up to date.
- **`ContinuousIntegrationBuild`** (in `Directory.Build.props`, under GitHub Actions only): CI builds map source paths to `/_/`, so the packed assemblies are deterministic and package explorers say so. Local builds keep real paths for debugging.
