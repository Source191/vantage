# Proposal

## Why

The README is both the user guide and the nuget.org package readme, yet its last third (Developing Vantage, Continuous integration, Working with OpenSpec, and the work-on-Vantage steps in Install) is for people changing Vantage, not using it. A contributor has no single place that explains the workflow, and the OpenSpec section says what the commands are without saying why every change goes through them or why the specs and the archive are kept for ever. The repository is about to be made public, and GitHub links a `CONTRIBUTING.md` from every new issue and pull request page.

## What Changes

- A new root `CONTRIBUTING.md` holding everything needed to develop Vantage: how to contribute (outside contributions welcome; open an issue first for anything beyond a small fix; every pull request needs the owner's approval; contributions under Apache 2.0, no CLA; security reports through `SECURITY.md`), setting up and the build and test loop, installing a local or CI build, the projects and their READMEs, a summary of the code rules with CLAUDE.md as the full rulebook, the OpenSpec workflow stage by stage (explore, propose, apply, update, sync, archive) and why the specs and the archive are kept for ever, the pull request checklist, the manual checks for tab and attach changes, continuous integration, versions and releases, and a closing "Releasing and maintaining" section for the owner-only operations (releases, nuget.org publishing, the Dependabot bump token, secrets, rulesets, the nightly Claude Code contract check).
- `README.md` loses the Developing Vantage, Continuous integration and Working with OpenSpec sections and their Contents entries, and the Install section's work-on-Vantage steps (local install, CI artefact, pack by hand); a short "Contributing" section links to `CONTRIBUTING.md` by its absolute GitHub URL, because the README is also the package readme on nuget.org.
- References to the moved sections are repointed: CLAUDE.md (the README as user guide, the "Developing Vantage" section that links the project READMEs, "the README's manual checks"), `.github\CODEOWNERS`, `.github\dependabot.yml`, the comments and error messages in `.github\workflows\check.yml` and `.github\scripts\Assert-VersionBumped.ps1`.

No behaviour of the tool changes and nothing under `src\` is touched, so `Version` stays as it is; `.openspec.yaml` declares `skip_specs: true`.

Out of scope: rewriting the code rules themselves or moving them out of CLAUDE.md (CONTRIBUTING summarises and links); the user-facing README sections that name a test or `tools\HeadlessTerminal` as evidence (What Vantage records, Updates, Troubleshooting), which stay; the in-flight changes' task lists that mention "the README's manual checks", which their own changes update; a pull request template; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Documentation only; `needs-input-notification`'s requirement on the README's Windows Terminal setup section is unaffected, as that section stays.

## Impact

- `CONTRIBUTING.md` (new), `README.md`, `CLAUDE.md`.
- `.github\CODEOWNERS`, `.github\dependabot.yml`, `.github\workflows\check.yml` (comments and one error message), `.github\scripts\Assert-VersionBumped.ps1` (comment and error message). Because `.github\` changes, the pull request runs the full `check` rather than the documentation-only path.
- `DocumentedMinimumShould` reads the README for the minimum Claude Code version; the Prerequisites section that states it stays.
