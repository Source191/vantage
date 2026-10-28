# Tasks

## 1. Release a prerelease as a GitHub prerelease

- [x] 1.1 In `release.yml`'s "Publish the release" step, build the `gh release create` arguments as an array and add `--prerelease` when `$env:VERSION` contains a hyphen (a semantic version's prerelease label; the csproj never carries build metadata); verify by running the step's script locally against `0.16.1` and `0.16.2-preview.1` with `gh` replaced by a function that prints its arguments, and `--prerelease` appears only for the second
- [x] 1.2 README, Releases: a `Version` with a prerelease label (`0.16.2-preview.1`) is released as a GitHub prerelease and never becomes "Latest"; verify the paragraph reads correctly beside `release.yml`

## 2. The first nuget.org prerelease

- [x] 2.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to `0.16.2-preview.1`; verify `.github\scripts\Assert-VersionBumped.ps1 -Base origin/main -Head HEAD` exits 0 and `dotnet build -c Debug` and `dotnet test -c Debug --no-build` pass
- [x] 2.2 README, Publishing to nuget.org and Install: a prerelease is published the same way, is hidden from search and from installs that name no version, installs with `--prerelease`, and an installed prerelease never checks for updates (update it with `dotnet tool update -g Source191.Vantage --prerelease`); verify `dotnet format --verify-no-changes` and `openspec validate mark-prerelease-releases` pass

## Workflow follow-up

- Archive the change in the same pull request, push, open the pull request and merge it once `check` passes.
- Confirm `release.yml` published `v0.16.2-preview.1` as a prerelease: `gh release view v0.16.2-preview.1 --repo Source191/vantage --json isPrerelease` answers true, and `v0.16.1` is still "Latest".
- Run `gh workflow run publish.yml --repo Source191/vantage -f tag=v0.16.2-preview.1`, watch it, and check that `dotnet tool install -g Source191.Vantage --prerelease` installs `0.16.2-preview.1` once nuget.org has indexed it.
