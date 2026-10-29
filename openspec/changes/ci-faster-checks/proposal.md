# Proposal

## Why

The required `check` job takes about 3.5 minutes per pull request (217 s in run 40869201276, typical of the last fifteen), and about 80 s of it is redundant: `setup-dotnet` downloads a 300 MB SDK the runner image already has (43 s), `dotnet format` repeats the style and analyser checks the build already enforces (41 s), and the Claude Code install waits behind the build (26 s). Six of the last forty merged pull requests touched only `openspec\` or Markdown files and still ran all of it, and the non-blocking `handoff` job (160 s) keeps the pull request page spinning after `check` is green.

## What Changes

- **No `setup-dotnet`:** the `build-test-pack` composite action, the `handoff` job and `claude-contract.yml` use the runner image's .NET SDK, chosen through `global.json` (10.0.301, `latestMinor`), and print `dotnet --version` so each run records which SDK it used.
- **Whitespace-only format check:** `dotnet format whitespace --verify-no-changes --no-restore` replaces the full `dotnet format`, because `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors` in `Directory.Build.props` already fail the build on every style and analyser rule at warning or above.
- **Documentation-only fast path:** on a pull request whose every changed file is under `openspec\` or is a `.md` file, `check` passes after the version check without building, and `handoff` skips its work. Pushes to `main` always run everything, and a change under `.github\` is never documentation.
- **Claude Code installs in the background:** `check` and `handoff` start the installer right after checkout and wait for it only when it is needed, falling back to a foreground install if the background one did not finish.
- **Superseded runs are cancelled:** a concurrency group per pull request cancels the older run when a new commit is pushed; runs for pushes to `main` are never cancelled.
- **`handoff` builds only the tool:** `dotnet build src\Source191.Vantage -c Release` instead of the whole solution with its tests.
- **Docs:** the README's "Continuous integration" section and CLAUDE.md's CI line describe the fast path and the format check.
- **No Vantage version raise:** nothing under `src\` changes. `skip_specs: true`: tooling only, no behaviour of the tool changes.

The target is a `check` job of about 2 minutes for a code pull request and under 30 s for a documentation-only one, with `handoff` at about 90 to 100 s.

Out of scope: larger or paid runners, a NuGet package cache (Windows extraction eats most of the 16 s restore it would save), a stub `claude.exe` for the smoke check (the smoke check is the one test of the bits users install), splitting `check` into parallel jobs (branch protection requires the single `check` job), changes to `release.yml` and `publish.yml` beyond what they inherit from the composite action, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: the change is CI tooling, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **CI:** `.github\actions\build-test-pack\action.yml` (no `setup-dotnet`, the SDK line, the format step), `.github\workflows\check.yml` (concurrency, the documentation gate, the background install in both jobs, `handoff`'s build and checkout depth), `.github\workflows\claude-contract.yml` (no `setup-dotnet`, the SDK line), a new `.github\scripts\Test-DocumentationOnly.ps1`. `release.yml` inherits the composite's changes.
- **Docs:** README's "Continuous integration" and "The hand-off job" paragraphs; CLAUDE.md's CI line.
- **Branch protection:** unchanged; `check` stays the one required job and still reports on every pull request.
- **Other open changes:** `dependabot-version-bump` adds a step to `check.yml` before the version check and `windows-only-package` extends the composite's "Read the version and find the package" step; neither overlaps these edits, so whichever merges later rebases without a conflict in substance. The documentation gate runs after the version check, so the Dependabot step is unaffected.

<!-- docs-only probe, never merged -->
