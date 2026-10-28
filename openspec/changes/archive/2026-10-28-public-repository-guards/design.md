# Design

## Context

Read on 28 Oct 2026 with `gh`, while the repository was still private:

- **People:** `simonvane` is the only member of the `Source191` organisation and the only collaborator (admin). There are no deploy keys, webhooks or teams.
- **Apps:** `claude` and `coderabbitai` are installed on all of the organisation's repositories, each with contents, pull requests, workflows and (for `claude`) Actions write. `aikido-security` is read only. CodeRabbit is being uninstalled by hand.
- **main:** the organisation ruleset "Require PRs on default branch" needs a pull request with 0 approvals, blocks deletion and force pushes, and has no bypass actors. Classic branch protection requires `check` with up-to-date branches, and `enforce_admins` is off. `allow_auto_merge` is on.
- **Workflows:** `release.yml` creates `v<version>` tags through `gh release create` with `GITHUB_TOKEN` (github-actions). `publish.yml` is `workflow_dispatch` only, with `id-token: write`, and the nuget.org policy matches owner, repository and workflow file but no environment.
- **Not available while private:** fork pull request approval (422), private vulnerability reporting (404), and secret scanning, which is off.

## Goals / Non-Goals

**Goals:**
- Nothing reaches `main` or nuget.org without Simon's own approval, whoever or whatever opens the pull request or starts the run.
- His own day-to-day flow (open a pull request, wait for `check`, merge) keeps working without a second person.

**Non-Goals:**
- Guarding against Simon's own token. Claude Code sessions on his machine use his admin `gh` login, so they can do whatever he can; their permission prompts are the control there.

## Decisions

- **A repository ruleset "Owner approves main", rather than editing the organisation ruleset.** It targets `~DEFAULT_BRANCH` with a `pull_request` rule: 1 approval, code owner review, stale reviews dismissed. Its one bypass actor is the repository admin role (`RepositoryRole` 5) in `pull_request` mode, so Simon merges his own pull requests through the bypass but cannot push to `main` directly. The organisation ruleset is left alone because it applies to every Source191 repository.
  - *Rejected: 1 approval with no bypass.* GitHub never lets authors approve their own pull requests, so a sole maintainer could not merge anything.
  - *Rejected: 0 approvals (today).* An app token can open and merge a pull request.
- **CODEOWNERS is `* @simonvane`, with code owner review required.** Only his approval satisfies the rule, so an approving review from an app or a future collaborator does not count. Dependabot pull requests wait for him too.
- **The `nuget` environment gates publishing.** Simon is its required reviewer, with `prevent_self_review` off because he is the one who starts the run. Its deployment policy is custom with the single branch `main`, and administrators cannot bypass it. `publish.yml`'s job names `environment: nuget`, and the nuget.org policy sets Environment `nuget`, which the NuGet docs describe as restricting the policy to runs in that environment. A run started any other way (a branch copy of the workflow, or an app dispatching it) gets no key. It either waits for his approval or fails the token exchange.
  - *Rejected: a `v*` tag policy.* An environment's policy checks the ref the run starts from, not the `tag` input. Today's flow starts from `main` with the tag typed in, so a tag-only policy would refuse it unless publishing moved to dispatching from the tag.
  - *Rejected: restricting who can dispatch.* GitHub has no such setting beyond write access, and the `claude` app has Actions write.
- **The `claude` app is scoped to selected repositories.** An app with workflows write can push a workflow to a branch, and that run gets the repository's secrets, `CLAUDE_CODE_OAUTH_TOKEN` among them, which no environment protects. Limiting the installation is the only control over that, and Simon does it in the organisation's GitHub Apps settings.
- **The "Release tags" ruleset protects `refs/tags/v*` against update and deletion, not creation.** `release.yml` creates the tags as github-actions, and allowing that bypass would let any workflow create one. A stray new tag cannot publish because the environment still needs Simon. Repository admins bypass it at all times.
- **Settings are applied one `gh` command at a time, and each is read back.** The commands live in tasks.md so the sequence can be repeated. The post-public settings come straight after the flip, because GitHub rejects them while the repository is private:
  - **Interaction limit:** `PUT repos/Source191/vantage/interaction-limits` with `limit=collaborators_only`, `expiry=six_months`.
  - **Fork pull request approval:** `PUT .../actions/permissions/fork-pr-contributor-approval` with `approval_policy=all_external_contributors`.
  - **Private vulnerability reporting:** `PUT .../private-vulnerability-reporting`.
  - **Scanning:** `PATCH repos/Source191/vantage` with `security_and_analysis` turning on secret scanning and push protection, then `PUT .../vulnerability-alerts`.

## Risks / Trade-offs

- [Auto-merge never fires for Simon's own pull requests, because the review requirement is only met by the bypass] → he merges with the bypass (the web checkbox, or `gh pr merge --admin`) once `check` is green. `allow_auto_merge` stays on for pull requests he has approved.
- [The environment is referenced before it exists, and GitHub creates an unprotected one on first use] → the environment is created and read back before the `publish.yml` change merges.
- [GitHub refuses required reviewers on a private repository on the Team plan (422, found while applying), so until the flip the environment has only its `main` rule] → the reviewer is the first setting applied after the flip (tasks 3.2). Until this pull request merges, `main`'s `publish.yml` does not use the environment at all, which is the same as before this change.
- [The nuget.org policy may still be in its seven-day pending window (created against a private repository), and lapse before the first publish] → the policy page shows its state; restarting the window is one click, and the first successful publish activates it permanently.
- [The interaction limit also stops honest strangers opening issues or pull requests for six months] → it can be lifted early with `DELETE .../interaction-limits`, and SECURITY.md's private reporting stays open to everyone.
- [Up-to-date branches plus dismissed stale reviews mean every rebase of a Dependabot pull request dismisses Simon's approval] → he approves again after the rebase; this is the cost of approving exactly what merges.
- [Simon's local admin token bypasses both rulesets] → accepted (see Non-Goals).

## Migration Plan

Everything happens while this change's pull request is open, so it can be archived in the same pull request (tasks.md):

1. Open the pull request, then create the `nuget` environment before anything can run the changed `publish.yml`.
2. Set the nuget.org policy's environment to `nuget`.
3. Turn off wiki and projects, and create both rulesets. Creating the owner ruleset before CODEOWNERS reaches `main` is safe: with no CODEOWNERS file the code owner part has nothing to require, and only the admin bypass can satisfy the 1-approval part.
4. Make the repository public, add the environment's required reviewer, apply the other post-public settings and read everything back.
5. Archive the change, then merge with the admin bypass once `check` is green.

Rollback: every setting is reversible with `gh` (delete a ruleset, environment or interaction limit, or switch a setting back off). Going public is not reversible in effect, because clones and forks made while public remain.
