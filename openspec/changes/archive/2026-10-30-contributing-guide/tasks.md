# Tasks

## 1. The contributing guide

- [x] 1.1 Write `CONTRIBUTING.md`: how to contribute (issue first, owner approval, Apache 2.0, no CLA, security), setting up, the build and test loop, local and CI installs, the projects, the code rules in summary with CLAUDE.md as authority, the OpenSpec stages and why the specs and archive are kept, the pull request checklist, manual checks, continuous integration, versions, and the "Releasing and maintaining" section. Verify every command in it matches CLAUDE.md and the workflows, and every relative link resolves to a file in the repository.

## 2. The README as a user guide

- [x] 2.1 Remove Developing Vantage, Continuous integration and Working with OpenSpec and their Contents entries, and the Install section's work-on-Vantage steps; add a "Contributing" section and Contents entry linking `CONTRIBUTING.md` by absolute URL. Verify `grep` finds no `#developing-vantage`, `#continuous-integration` or `#working-with-openspec` anchors left in the README, and `DocumentedMinimumShould` still passes (`dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug -- --filter-class *DocumentedMinimumShould`).

## 3. Repointed references

- [x] 3.1 Update CLAUDE.md, `.github\CODEOWNERS`, `.github\dependabot.yml`, `.github\workflows\check.yml` and `.github\scripts\Assert-VersionBumped.ps1` to name `CONTRIBUTING.md` sections instead of the README's. Verify a search outside `openspec\changes\archive` for "see README", "README, Continuous", "README.md, Version bumps" and "Developing Vantage" finds nothing stale, and `openspec validate contributing-guide` passes.

## Workflow follow-up

- Archive the change in the same pull request (`/opsx:archive contributing-guide`); with `skip_specs: true` nothing merges into `openspec\specs`.
