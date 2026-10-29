# Source191.Vantage

The packed tool: `Program.cs`, the only composition root, wires Core, Infrastructure and Tui together, and packing includes those three projects. The user guide is the repository's [README](../../README.md), which is also the package readme on nuget.org; `THIRD-PARTY-NOTICES.txt` travels beside the tool, as the licences of the packed dependencies ask.

## Package settings

- **`SatelliteResourceLanguages` `en`:** Vantage is English only, so the translated resources of System.CommandLine and Terminal.Gui are neither built nor packed, and those libraries' own messages stay English on every Windows display language.
- **`IncludeSourceRevisionInInformationalVersion` false:** `vantage --version` prints exactly the package version that nuget.org and the GitHub Release show, with no `+<commit>`. The commit is still recorded in the package's repository metadata. The `check` job asserts this on the installed tool.
- **`PackageReleaseNotes`:** a link to the version's GitHub Release, where `release.yml` generates the notes; there is no changelog file to keep up to date.
- **The `RemoveNonWindowsNativeAssets` target:** Terminal.Gui depends, through TextMateSharp, on Onigwrap, whose `libonigwrap` ships for thirteen platforms; the target drops every published file whose runtime identifier is not a Windows one, so the package keeps only `win-x64`, `win-arm64` and `win-x86`, and the shared pack step fails if any other platform's assets come back. RID-specific tool packages were tried instead and rejected: they install a `vantage.cmd` shim and a versioned executable path that breaks `U` and the terminal profile after every update.
- **`ContinuousIntegrationBuild`:** set under GitHub Actions in the root `Directory.Build.props`, which explains it, so the packed assemblies are deterministic.
