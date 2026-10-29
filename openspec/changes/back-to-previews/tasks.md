# Tasks

## 1. Version

- [ ] 1.1 Set `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to `0.18.0-preview.1`. Verify `.github\scripts\Assert-VersionBumped.ps1 -Base origin/main -Head HEAD` passes and `dotnet build -c Debug` is clean.

## 2. GitHub

- [ ] 2.1 Mark the v0.17.0 GitHub Release as a pre-release (`gh release edit v0.17.0 --prerelease`). Verify `gh release view v0.17.0 --json isPrerelease` answers true.

## Workflow follow-up

- Archive the change in the same pull request (`skip_specs`, so no spec sync).
