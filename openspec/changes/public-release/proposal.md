# Proposal

## Why

Vantage is ready to be used by people other than its author, but it is a private repository whose packages exist only as GitHub Release assets, installed with `--add-source`. Twelve changes are planned and will be implemented before one manual test at a real terminal, so the order they merge in, and the steps from the last merge to a public repository and a package anyone can install with `dotnet tool install -g Source191.Vantage`, need one place to live.

## What Changes

- **The pull request sequence** for every open change, P0 to P14, in `design.md` and tracked in `tasks.md`: which changes stack, which run side by side, and the order they merge.
- **CI for stacked pull requests:** `check.yml` runs on pull requests to any branch, not only `main`.
- **The package is ready to publish:** licence expression, readme, icon, project and repository URLs, tags and copyright in the tool's csproj; a `THIRD-PARTY-NOTICES.txt` packed with it; `SECURITY.md`; Dependabot for NuGet and GitHub Actions.
- **A hand-run `publish.yml`** pushes the nupkg of an existing GitHub Release to nuget.org through Trusted Publishing, so only a tested release is ever published and no API key is stored. `release.yml` is unchanged: every merge that raises `Version` still makes a GitHub Release.
- **One manual test session** before going public, covering the README's manual checks, each change's own terminal checks and the `wt-title-escaping` spike. Until then the README's rule that tab and attach changes are checked at a terminal before merging is waived for the changes in this sequence.
- **Going public:** a check of the history, the repository made public, the first package published, and an install from nuget.org on a clean profile.
- **README:** Install becomes `dotnet tool install -g Source191.Vantage`; Continuous integration describes `publish.yml`; the Roadmap loses the nuget.org item.

Out of scope: version 1.0 (the first public release stays 0.y); winget or other installers; signing the package; any behaviour change, which belongs to the changes in the sequence; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Publishing and CI change nothing Vantage does; `.openspec.yaml` declares `skip_specs: true`. `update-notice`, the one new behaviour this release adds, is its own change.

## Impact

- **CI:** `.github\workflows\check.yml` (trigger), new `.github\workflows\publish.yml`, new `.github\dependabot.yml`, the comment in `release.yml`.
- **Package:** `src\Source191.Vantage\Source191.Vantage.csproj`, a package icon, new `THIRD-PARTY-NOTICES.txt`.
- **Repository:** new `SECURITY.md`; GitHub settings (visibility, private vulnerability reporting, approval for outside contributors' workflow runs); a nuget.org account with a Trusted Publishing policy.
- **Docs:** README (Install, Continuous integration, Roadmap), CLAUDE.md (publishing), every open change's ordering note.
- **Version:** a patch bump in P11, because the csproj changes.
