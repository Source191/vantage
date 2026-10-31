# Tasks

## 1. Check the scenarios against the console

- [ ] 1.1 Check each scenario of the modified requirement against the code on main: `WhereLabelShould`, `FolderLabelShould` and `FolderLabelsShould` for the texts they pin, and `SessionsScreenDefinition.Repository` and `WorktreesScreenDefinition`'s WORKTREE cell for which label each cell builds. Where a scenario is not pinned by a fact (the session in a worktree, the repository subfolder, the main checkout), check it in a headless capture: seed Northwind fixtures as the `check` job does, `dotnet run --project tools\HeadlessTerminal -- drive` on both tabs, then `grid`. Verify every scenario's text appears in the capture or a fact, and record here any that does not, with the console's actual text, before changing the spec.
- [ ] 1.2 `CONTRIBUTING.md`'s manual check for a new worktree session reads its REPOSITORY cell (the repository) and the worktree's WORKTREE cell (the repository › the slug) in place of "WHERE cell reads the repository › the worktree's slug". Verify by reading it against the spec delta and checking no em dash was introduced.
- [ ] 1.3 `openspec validate correct-folder-label-spec --strict` passes, the requirement's description is 500 characters or fewer, and `git diff --stat origin/main` shows only `openspec\` and `CONTRIBUTING.md` (no version raise).

## Workflow follow-up

- Archive the change in the pull request that completes it, which syncs the delta into `openspec\specs\folder-actions\spec.md`.
