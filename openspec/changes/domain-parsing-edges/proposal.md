# Proposal

## Why

The code review of 0.20.0-preview.7 found five edges where a Domain rule or a CLI reading is looser or less consistent than it says it is: a GitHub https remote with a user name before the host (`https://northwind@github.com/…`, as Git Credential Manager writes it) is an unknown host while the same shape on Azure DevOps is read; `ReleaseVersion.TryParse` lets a leading zero through, so `1.0.0-01` and `1.0.0-1` compare equal but are not equal; `WhereLabel.Fit` overruns a width below 5 and emits a dangling `›` for an empty place; `BatchFileArgument.IsSafe`, the one rule for what az.cmd may be given, accepts a trailing newline; and `ClaudeCli` keeps its own copies of what a job id and a version look like beside `JobId` and `ClaudeVersion`. None reaches a user today except the first, which hides a row's pull request; all are a few lines and worth closing before go-live.

## What Changes

- A GitHub https remote with a user name before `github.com` is read as that owner and repository, so its pull requests are looked up with `gh` as an Azure DevOps remote's are with `az`; the user name is dropped, never kept.
- `ReleaseVersion.TryParse` refuses a numeric identifier with a leading zero (`01.0.0`, `1.0.0-01`) and an empty prerelease identifier (`1.0.0-a..b`), as semantic versioning 2.0 does, so `CompareTo` and `Equals` agree; `1.0.0-0` and `1.0.0-01a` still read.
- `ReleaseVersion` hides its constructor behind `TryParse` and a `From(text)` door for text Vantage trusts (`ClaudeCodeSupport.Minimum`), as every other Domain value does; an Architecture fact extends "no public constructor" from the text values to every Domain record with a `TryParse`.
- `WhereLabel.Fit` never answers more characters than its width and never a `›` without a place; the literal `3` becomes a named least width for a middle elision.
- `BatchFileArgument.IsSafe` is a `SearchValues<char>` check instead of a regex, which also refuses a trailing newline.
- `ClaudeCli` reads the job id in a `backgrounded` line and the version in `--version` output by splitting into words and asking `JobId.TryParse` and `ClaudeVersion.TryParse`, leaving one rule per fact; its two regexes and the comment about the two rules agreeing go.
- `RemoteRepository`'s and `WorktreeSlug`'s regexes are kept: `Uri` rejects the three scp-style remotes (`git@github.com:owner/name`), so a replacement is some forty lines of split-and-check against two declarations, and a span loop over a slug reads worse than one pattern.

Out of scope: banning `Regex` with an analyser (the review's D-20 and I-03, a later decision once the kept sites are the only ones); any other remote shape (GitHub Enterprise hosts, `http://`); the other review findings, each its own change; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `pull-requests`: the host comes from the origin remote; a user name before the host in an https URL is accepted for GitHub as it already is for Azure DevOps.

## Impact

- `src\Source191.Vantage.Core\Domain\RemoteRepository.cs` (the GitHub pattern), `ReleaseVersion.cs` (identifier checks, the private constructor, `From`), `ClaudeCodeSupport.cs` (`Minimum` through `From`), `WhereLabel.cs` (`Fit`, `ElideMiddle`, `CutEnd`, the named constant), `BatchFileArgument.cs` (`SearchValues`, no longer `partial`).
- `src\Source191.Vantage.Infrastructure\Claude\ClaudeCli.cs` (word splits in place of `LeadingVersion` and `BackgroundedLine`, no longer `partial`).
- Tests: `RemoteRepositoryShould`, `ReleaseVersionShould`, `WhereLabelShould`, `BatchFileArgumentShould`, `ClaudeCliShould`, `Architecture\ParsingShould`; every `new ReleaseVersion(…)` in `ReleaseVersionShould`, `NewestReleaseShould`, `UpdateCheckRuleShould`, `ClaudeCodeSupportShould` and `JsonFileStoreShould` goes through `ReleaseVersions.Version` or `ReleaseVersion.From`.
- No wording, key, dialog, state file or command line changes; `Version` raised in the implementing pull request (patch: fixes with no new behaviour but the one remote shape).
