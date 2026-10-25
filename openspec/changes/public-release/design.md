# Design

## Context

See proposal.md, Why. Every pull request that changes the packaged tool raises `<Version>` in `src\Source191.Vantage\Source191.Vantage.csproj` (`Assert-VersionBumped.ps1`), branch protection requires `check` and "branches up to date", and the repository merges with merge commits. `check.yml` runs only on pull requests to `main`. `release.yml` makes a GitHub Release with the nupkg whenever a push to `main` raises `Version`. OpenSpec 1.15.0 has no field for ordering between changes (a `dependsOn:` key in `.openspec.yaml` fails `validate --strict`), so the order is written here and cited by P-number in each change's "Workflow follow-up".

## Goals / Non-Goals

**Goals:** a merge order that respects every change's stated dependencies and keeps rebases small; two streams of work that can run at once in two worktrees; the package published to nuget.org is byte for byte the one tested.

**Non-Goals:** testing each pull request at a terminal as it merges; automating the visibility flip or the nuget.org account.

## Decisions

### The sequence

| P | Change | Stream | Branches from | Bump | Model | Effort |
|---|---|---|---|---|---|---|
| P0 | public-release: CI for stacked PRs | either | main | none | n/a (done) | n/a |
| P1 | remove-session-descriptions | gate | main | minor | Sonnet | medium |
| P2 | unreachable-arms | refactors | main | patch | Sonnet | medium |
| P3 | text-value-doors | refactors | P2 | patch | Opus | high |
| P4 | facts-not-sentences | refactors | P3 | patch | Sonnet | medium |
| P5 | from-factory-spike | refactors | P4 | patch, or closed unmerged | Sonnet | low |
| P6 | header-and-help | features | main | minor | Sonnet | high |
| P7 | settings-and-worktree-root | features | P6 | minor | Opus | high |
| P8 | folder-column-and-open-keys | features | P7 | minor | Sonnet | high |
| P9 | pull-request-column | features | P8 | minor | Opus | high |
| P10 | windows-terminal-setup | standalone | main | minor | Sonnet | high |
| P11 | public-release: package ready | release | main | patch | Sonnet | medium |
| P12 | update-notice | release | main | minor | Opus | high |
| P13 | public-release: final manual test and fixes | release | main | as each fix needs | you | n/a |
| P14 | public-release: go public, archive | release | main | none | you | n/a |

- **P1 first, alone.** Five changes name `remove-session-descriptions` as a prerequisite; it touches the wizard, the launch request and the state store that the rest rework.
- **The refactors stream merges before the features stream grows.** `text-value-doors` changes how every text value is parsed across nearly thirty source files, and `from-factory-spike`, if merged, changes every construction site. Feature branches written against the old shapes would each need a large rebase, so P2 to P5 merge first and P5 is decided as soon as P3 or P4 is green.
- **Two streams at once.** While the refactors stream is in flight, the second worktree takes P10 (no overlap beyond `Program.cs` and the README) and then P6 (its overlap is `Wording` and `ListSelection`, both small to rebase). P7 to P9 start once P3, and P5 if merged, are on `main`.
- **Stacks within a stream.** A stacked branch raises the version above its parent's, so when its parent merges and GitHub retargets it to `main` its version is already higher and no new bump is needed. Branches in different streams collide on the `<Version>` line: when one stream merges, every open branch of the other needs one re-bump, so a stream's stack is merged in one burst rather than one PR at a time.
- **Version is decided at merge time:** the next patch or minor after `main`'s when the branch is brought up to date, never a number fixed in advance.
- **P12 after P6 and P7.** It reads the header line `header-and-help` adds and the settings holder `settings-and-worktree-root` adds; it needs nothing from P8 to P10.

### Model choice

Each change is implemented by one session, `/opsx:apply` from start to finish, on the model and effort in the table: never a subagent per task, because every task in a change needs the same context (CLAUDE.md, the specs, the code it touches) and splitting them re-reads it each time and breaks the red, green, refactor thread. Speed comes first from the two streams running at once, then from the model.

- **Sonnet 5.5 is the default.** Every change is fully specified, and most follow a pattern the code already has: a new key is four pieces, a footer case is a `Wording` line and a fact. Effort is low for mechanical work (P5's construction sites), medium for small or deletion-only changes, high where Terminal.Gui or Windows Terminal behaviour is involved (P6, P8, P10).
- **Opus 5.5 where a mistake spreads.** P3 reshapes how every text value is parsed and everything after it builds on that; P7 adds a dialog reducer, a config migration and a placement change; P9 adds two CLI adapters and a cache off the UI thread; P12 hands Vantage over to a tab that updates it while it is locked.
- **Haiku is not used.** Warnings as errors, IDE0022, the layering test and the reflection facts would send it round the build loop more often than it saves.
- **Review on Opus.** A Sonnet change gets an Opus `/code-review` before its pull request is opened.
- **The `wt-title-escaping` spike is P13's,** run by the product owner at the terminal; a session at Sonnet low records the result.

### CI for stacked pull requests (P0)

`check.yml`'s `pull_request` trigger loses `branches: [main]`. The version step already compares the merge ref's two parents, so a stacked pull request is compared with its parent branch; branch protection still guards only `main`. Alternative: open every pull request against `main` and rebase after each merge. Rejected: a stacked pull request's diff would include its parents' commits until they merge.

### Manual checks are waived until P13

The README asks for a terminal check before merging any pull request that changes `wt.exe`, tabs or attach (P6's tab title, P10's tab profile, P12's update tab). In this sequence those pull requests merge on CI and the `handoff` job alone, and P13 runs every check before anything is published. The GitHub Releases made in between are private provenance, not distribution, so an untested release reaches nobody.

### Publishing (P11, P14)

- **`publish.yml`**, `workflow_dispatch` with a `tag` input: `gh release download <tag> --pattern *.nupkg`, then `NuGet/login@v1` (job permission `id-token: write`, the nuget.org profile name from a `NUGET_USER` secret) for a one-hour key, then `dotnet nuget push --source https://api.nuget.org/v3/index.json`. It never builds, so what is pushed is the release asset that was tested. Alternative: push from `release.yml` on every merge. Rejected: nuget.org versions cannot be replaced, and every merge would publish before P13.
- **Trusted Publishing policy** on nuget.org: owner `Source191`, repository `vantage`, workflow `publish.yml`.
- **Package metadata:** `PackageLicenseExpression` `Apache-2.0`, `PackageReadmeFile` (the README), `PackageIcon`, `PackageProjectUrl` and `RepositoryUrl` (the GitHub repository), `PackageTags` (`claude-code;worktrees;windows-terminal;tui;dotnet-tool`), `Copyright`. The README's relative links and images do not render on nuget.org; the package readme is the README as it is, checked on the nuget.org preview.
- **Licence: Apache-2.0 stays.** It grants patents and states how contributions are licensed, so no contributor agreement is needed. The direct dependencies are MIT (Terminal.Gui, Interop.UIAutomationClient, Microsoft.Extensions.*); Terminal.Gui brings transitive ones that are permissive but not all MIT (Markdig is BSD-2-Clause; ColorHelper, Onigwrap with its native Oniguruma, TextMateSharp, Wcwidth and the file-system abstraction packages are to be checked). All permissive licences ask for their notice to travel with the copies, so `THIRD-PARTY-NOTICES.txt` lists each one and is packed. Alternative: MIT, the commoner .NET choice. Not taken: it adds nothing here and drops the patent grant.
- **First public version stays 0.y.** P9 moves prune from `p` to `P`, a break; 1.0 waits until keys and state files have been stable for a while.

## Risks / Trade-offs

- [A fix found in P13 lands in a change already archived] → it is a new small change or a fix pull request, with its own bump; the archived change's text stays as merged.
- [A stack's bottom pull request needs rework after review] → the branches above it rebase; the streams are at most four deep, which keeps that bounded.
- [The history holds something not meant for the public] → P14 checks author emails and runs a secret scan before the flip, while rewriting history is still painless.
- [Outside contributors' pull requests run workflows] → GitHub's approval for first-time contributors stays on; no workflow uses `pull_request_target`, so forks never see `CLAUDE_CODE_OAUTH_TOKEN` or `NUGET_USER`.

## Migration Plan

Installs made with `--add-source` from a GitHub Release keep working; `dotnet tool update -g Source191.Vantage` moves them to nuget.org's copy once a higher version is published there. Rollback of a bad publish is unlisting the version on nuget.org and publishing a fixed patch.
