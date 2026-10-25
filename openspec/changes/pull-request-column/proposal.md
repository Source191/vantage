# Proposal

## Why

Whether a session's branch already has a pull request, and whether that pull request is still open or has merged, decides what to do next with the session and its worktree, yet finding out means leaving Vantage for a browser or a CLI. Repositories live in GitHub and in Azure DevOps, so the answer has to come from both.

## What Changes

- A PR column in the sessions view and the worktrees view: `#123 open` for GitHub (open, draft, merged, closed) and `!123 active` for Azure DevOps (active, draft, completed, abandoned); blank when the branch has none or it cannot be looked up.
- `p` opens the selected row's pull request in the default browser, in both views; with none, the footer says why: no origin remote, a host Vantage does not know, the CLI (`gh` or `az`) missing, not signed in, no pull request for the branch, or the lookup still running.
- The host comes from the repository's `origin` remote, parsed into a GitHub repository (owner, name) or an Azure DevOps repository (organisation, project, name); any other remote reads as no host.
- Lookups never delay the rows. One listing call per repository per three minutes (`gh pr list` takes about 1.8 s and `az repos pr list` about 4.2 s on this machine, so a call per row is not viable), run off the UI thread; rows show without a PR until the first answer lands and a later refresh picks it up. A branch with an open pull request shows it; otherwise its most recent one.
- **BREAKING**: the worktrees view's prune key moves from `p` to `P`, so `p` means "pull request" in both views. Prune acts on a whole repository, like the other capital-letter keys.

Out of scope: creating, approving or commenting on pull requests; build or review status in the column; hosts other than GitHub and Azure DevOps (GitLab, Bitbucket, GitHub Enterprise Server); signing in to `gh` or `az` for the user; per-cell colour for the state (the table colours whole rows); multiple Claude accounts, which is a later change.

Depends on `folder-column-and-open-keys`, whose `IDesktop.OpenAsync` opens the URL and whose column layout this change extends.

## Capabilities

### New Capabilities

- `pull-requests`: finding the pull request for each listed branch on GitHub or Azure DevOps, showing it in both tables, and opening it in the browser.

### Modified Capabilities

None. The prune requirement in `worktree-management` names "the prune key" rather than a letter, so the move to `P` changes the README, help and key hints, not a requirement.

## Impact

- `Core`: Domain gains the remote parser, the pull request record with its two hosts' states, and the branch-matching rule; Ports gains a nullable `IGit` query for the origin URL and an `IPullRequests` port; Application gains a cached pull request catalogue, a `PullRequest` field on `SessionRow` and `WorktreeRow`, the `p` reducer cases, footers, a handler, an outcome and their words.
- `Infrastructure`: `GitCli` reads `git remote get-url origin`; new GitHub and Azure DevOps adapters run `gh` and `az` through `IProcessRunner`.
- `Tui`: a PR column in both screen definitions, the `p` and `P` key hints.
- Tests: Domain facts for the parser and matching, adapter tests against `FakeProcessRunner` with recorded JSON, catalogue tests with `FakeTimeProvider`, reducer, handler, wording and dispatcher facts; a `FakePullRequests` in TestDoubles.
- Docs: README columns, keys and a note on signing in to `gh` and `az`; the version rises by a minor step.
