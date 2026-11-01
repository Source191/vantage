# Maintaining Vantage

This guide is for the owner: how a merge becomes a release, how a release reaches nuget.org, and the jobs and secrets behind them. Contributors need only [CONTRIBUTING.md](CONTRIBUTING.md).

## Releases

`.github\workflows\release.yml` runs on every push to `main` and is a no-op unless that push changed `Version`. When it did, a `build` job with read rights only builds, tests and packs again and hands the package on as an artefact; a `release` job, which checks nothing out and builds nothing, is the only one with the rights to write and sign: it downloads that artefact, attaches a signed build provenance attestation to it (`gh attestation verify <nupkg> --repo Source191/vantage` checks which commit and workflow built it), and publishes a GitHub Release tagged `v<version>` with the `.nupkg` attached. Immutable releases are off for the repository (checked 1 November 2026: `gh api repos/Source191/vantage/immutable-releases` answers `enabled: false`), so a release's assets could be replaced; never do so: a fix is a new version. A version with a prerelease label (`0.16.2-preview.1`) is published as a GitHub prerelease, so it never becomes the Latest release. A release is not published to nuget.org: that is a separate, manual step, so a package is tried by hand before it goes where a version can never be replaced. The "Release tags" ruleset stops anyone but the owner moving or deleting a `v*` tag.

## Publishing to nuget.org

`.github\workflows\publish.yml` is run by hand (Actions, "publish", with the release tag, e.g. `v0.5.0`). It downloads that GitHub Release's `.nupkg`, verifies its provenance attestation (`gh attestation verify` with `--signer-workflow` naming `release.yml` and `--source-ref refs/heads/main`, so a package swapped on the release or attested anywhere else fails the run) and pushes it to nuget.org without building anything, so what is published is the package that was built, tested and released from `main`.

- **No stored API key:** the `NuGet/login` action swaps the run's GitHub identity for a one-hour key through a Trusted Publishing policy on nuget.org (owner `Source191`, repository `vantage`, workflow `publish.yml`, environment `nuget`); the nuget.org profile name is the `NUGET_USER` repository secret.
- **The `nuget` environment:** accepts only runs started from `main` and waits for the owner to approve each one (Actions, the waiting run, Review deployments), so a copy of the workflow on a branch, or a run started by an app, never gets a key.
- **Mistakes:** nuget.org versions cannot be replaced, only unlisted, so a bad publish is unlisted and fixed by a higher patch version.
- **Prereleases:** published the same way; nuget.org leaves them out of search unless prereleases are included, and `dotnet tool install` takes one only with `--prerelease` or `--version`, which is how a version reaches nuget.org before it is offered to everyone.

## Reviews and merging

The "Owner approves main" ruleset requires a code owner's approval, and `.github\CODEOWNERS` names the owner for every path, so a pull request opened by anyone else (an app or Dependabot included) waits for that review. The owner merges their own pull requests through the ruleset's bypass (`gh pr merge --admin`), so auto-merge does not fire for them.

## Dependabot and the version bump token

Dependabot updates the NuGet packages and the actions' pins weekly, and security updates are on, so a dependency with a known vulnerability gets a pull request at once (the build's NuGet audit fails on it anyway). A Dependabot pull request that touches the tool is bumped for you: a step before the version check raises `Version` on the pull request's branch, commits "Raise Version to <version> for the dependency update" and pushes it with `DEPENDABOT_BUMP_TOKEN`, a fine-grained personal access token with Contents read and write on this repository, stored under Settings, Secrets and variables, Dependabot (a Dependabot run can read only those). The run for the pushed commit is the one branch protection reads; you still review and merge. When the token expires the step fails naming it: create a new one and replace the secret.

## Supply chain

Every action the workflows use is pinned to the commit of its release, with the version in a comment, so a moved tag cannot change what runs; Dependabot updates the pins, and the repository setting that requires such pins is turned on once every workflow on `main` carries them. The `CLAUDE_CODE_OAUTH_TOKEN` secret reaches the latest Claude Code CLI that the `handoff` and `claude-contract` jobs install from `claude.ai`, so it belongs to an account that has nothing else and is rotated like a password.

## The hand-off job

`handoff` installs Claude Code on the runner and runs the hand-off check from `tools\HeadlessTerminal` against a real throwaway session. It needs the `CLAUDE_CODE_OAUTH_TOKEN` repository secret (from `claude setup-token`); without one it skips with a notice, as it does for a documentation-only pull request. It builds only the tool (`src\Source191.Vantage`), because `check` builds and runs the tests, and installs Claude Code in the background while it builds. It never blocks a merge, because its keystroke timings were tuned on a laptop and a slow runner can fail it for reasons that are not bugs; read its `handoff-capture` artefact when it does.

The runner has no desktop, so the job seeds workspace trust for the checkout in its own `~\.claude.json`, which, with the nightly contract check, is the only place that record is ever written by a script. Vantage strips `CLAUDE_CODE_*` from the environment of the `claude` processes it starts (`InheritedEnvironment`), so the token reaches the throwaway session the harness starts but not Vantage's own `claude agents` and `claude attach`; the live runs show the attach reaches Claude's screen all the same. The check exits 3 when it does not: Claude's screen must be in the capture, not only Vantage's.

## The nightly Claude Code contract check

Claude Code's native installer updates the CLI in the background, so users soon run a newer CLI than Vantage was verified against. `.github\workflows\claude-contract.yml` runs every night at 03:17 UTC, and by hand from Actions, "claude-contract": it installs the latest CLI, runs the `ClaudeContract` tests (Vantage's own Claude adapters against that CLI: the version, a background session started with no prompt, listed with every field Vantage reads in a state it knows, then stopped and removed; and the commands and options in `claude --help` and `claude agents --help` against the snapshots in `test\Source191.Vantage.Infrastructure.Tests\Contract\Snapshots`), then the hand-off check. It needs the same `CLAUDE_CODE_OAUTH_TOKEN` secret and seeds trust the same way; without the secret it skips with a warning. It never runs on pull requests, so a CLI release never blocks a merge.

- **The claude-cli issue:** when the latest CLI fails to install or a contract test fails, a `report` job of its own, the only one with `issues: write`, reads the contract job's outcomes and its `contract-results` artefact and comments on the open issue labelled `claude-cli`, or opens one when none is open, with the CLI version, the run link and the failing tests' messages. It assigns a new issue to the GitHub login in the `CLAUDE_CLI_WATCHER` repository variable (Settings, Secrets and variables, Actions, Variables); an assignee is subscribed to the issue, so GitHub notifies them of every comment. Close the issue by hand once the fix is merged. The hand-off check marks the run but never raises the issue.
- **Accepting a snapshot:** a difference leaves `<name>.received.txt` in the run's `contract-results` artefact (and in the test output folder locally). Copy it over the snapshot in a change that re-checks the CLI facts in `.claude\rules\claude-code.md`, so a new flag is either used, deliberately ignored or recorded.
- **Never** add `--bg --resume --help` to the contract tests: it starts a session.