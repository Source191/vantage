# Design

## Context

See proposal.md for why. As of main at `356588b`: `GitWorktreeListing.ParseBlock` turns a `branch refs/heads/<name>` line into a `BranchName?` and ignores the `detached` line; `WorktreeInventory.Leftover` builds rows with no branch; `SessionRowEnricher.AskGitAsync` gets `string?` from `IGit.CurrentBranchAsync` (`git branch --show-current`, empty on a detached head, so the port answers null for detached and for failure alike) and keeps a `BranchName?` in its private `Location`. The branch is read by `PullRequestCatalogue.Find` and `RefreshStaleAsync`, `DetailLoader.WorktreeAsync` (merge state), `WorktreeRow.Facts` (the folder name stands in), `WorktreeRemover.RemoveThroughGitAsync` (the folder name stands in when it parses), `WordingOfDetail` ("none"), `ResumeChecklistDialog` ("-") and both views' filter fields. `WhereLabel.For` takes a `string? branch`, but both production callers pass none: `WorktreesScreenDefinition` passes `branch: null` and `SessionsScreenDefinition` builds the label directly with null.

## Goals / Non-Goals

**Goals:**
- No nullable reference type left on `SessionRow`'s and `WorktreeRow`'s branch, tab and session facts, on `GitWorktreeEntry.Branch`, `WorktreeRemovalFacts`, `WhereLabel` or `RowFilter`.
- Null from git met on the one line that reads it: `GitWorktreeListing` for the listing, `SessionRowEnricher` for `CurrentBranchAsync`.

**Non-Goals:**
- Telling a detached session from git failing to answer: the port cannot, and both show "none" today.

## Decisions

**`Head` has three cases, and `Unknown` is needed.** `OnBranch(BranchName Branch)`, `Detached`, `Unknown`. A leftover folder has no registration and so no head git can name; a session outside a repository, or one whose `CurrentBranchAsync` answered null, is not known to be detached; and a `branch` line whose text `BranchName.TryParse` refuses was dropped to null today. `WorktreeHealthKind` does not cover these, since it describes the folder's state, not its head. Every consumer that acts on a branch matches `Head.OnBranch { Branch: BranchName branch }`, which reads as plainly as today's `is not null`; the wording switches name all three cases and a discard arm that throws `UnreachableException`, as CLAUDE.md asks of a closed record hierarchy, and `Detached` and `Unknown` give today's text in every place. Alternative: `BranchName.None` as a null object (rejected: a blank branch name breaks `BranchName`'s own rule and would flow silently into `git branch -d` and the pull request lookups).

**`Head` stays on `SessionRow` for every session, inside a repository or not.** A session outside a repository has `Head.Unknown`, so `null-free-sessions` can build its `SessionLocation` on `SessionRow.Head` without another branch fact; this change leaves `SessionRowEnricher`'s private `Location` with its `Checkout?` and repository text for that change, holding a `Head` in place of its `BranchName?`.

**The port's record carries the `Head`; its commit text is renamed.** `GitWorktreeEntry(string Path, string Commit, Head Head, bool IsMain, string? LockReason, string? PrunableReason)`: the hash git prints on the `HEAD` line was named `Head` and is read nowhere, so it becomes `Commit` and the new property takes the name. `ParseBlock` gives `OnBranch` for a `branch` line that parses, `Detached` for a `detached` line, and `Unknown` otherwise (a bare repository's block, or a branch name that does not parse). The lock and prunable texts are `null-free-observations`'.

**`IGit.CurrentBranchAsync` keeps `string?`.** `SessionRowEnricher` turns it into `OnBranch` or `Unknown` on the line that receives it. Converting the port would touch `GitCli` and its integration tests for no gain in what the row can say, since the port cannot tell detached from failure.

**The folder-name stand-in becomes an explicit arm.** `WorktreeRemover` today uses the folder name as the branch when the row has none and the name parses; it now switches on the head: `OnBranch` gives its branch, `Detached` and `Unknown` give the folder name when it parses, as today. `WorktreeRow.Facts` keeps `Branch` text the same way: the branch for `OnBranch`, the folder name otherwise.

**`LiveTab` and `WorktreeSession` replace the pairs.** `LiveTab.None` and `LiveTab.Open(TabColour Colour)` live in `Domain` beside `TabColour`; `SessionRowEnricher` builds `Open` from the dictionary it already has. `OpenTabForSession(BackgroundSession Session, LiveTab Tab)` and `OpenTabHandler` switches when it is `Open`. `WorktreeSession.None` and `WorktreeSession.Running(SessionId Id, string Name)` live in `Domain` because `WorktreeRemovalFacts` holds one and `RemovalDecision` refuses on `Running`, giving `RemovalReason.LiveSession(running.Name)` as today. The name avoids `LiveSession`, which `RemovalReason` already nests.

**`WhereLabel` becomes `Outside(string Place)` and `Inside(string Repository, string Place)`, and its unused branch goes** (owner's decision, 31 October 2026). Neither production caller passes a branch: `SessionsScreenDefinition` builds the REPOSITORY cell with none, because the place and branch are the detail pane's, and `WorktreesScreenDefinition` passes `branch: null` for its WORKTREE cell. So `WhereLabel` drops its `Branch`, its bracket text and the branch arm of `Fit`, and `For(string repositoryName, string folderLabel, bool inRepository)` takes no branch and no `Head`; `Fit` and `Text` sit on each case. Every cell reads as today. `WhereLabelShould`'s facts that build a label with a branch go with the ability, and `CLAUDE.md`'s "repository, place and branch" becomes "repository and place". The folder-actions spec, which still describes a WHERE cell with the branch in brackets, is corrected by its own change, `correct-folder-label-spec`.

**Filter fields are non-nullable.** `RowFilter.Matches(IReadOnlyList<string>)` and `ListSelection`'s `Func<TRow, IReadOnlyList<string>>`; a missing branch or path adds no element (`.. head is Head.OnBranch on ? [on.Branch.Value] : []`), so matching is unchanged. `ConfigView.PathOf` keeps its `string?` until `null-free-outcomes` or `null-free-screens` changes it; its filter line spreads it the same way.

## Risks / Trade-offs

- [`SessionRow`'s positional constructor changes] → tests build rows through `SessionTestData` and `WorktreeTestData`, which change once; the eleven direct `new SessionRow(` calls in tests change with them.
- [A wording case is missed] → each wording switch names all three `Head` cases, so a fourth fails the build; `WordingOfDetailShould` keeps its expected texts.
- [The jump and filter code moved under `null-free-outcomes`] → the tasks name the behaviour, not line numbers; re-read `WorktreesView` before task 4.4.
