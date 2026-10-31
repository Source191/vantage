# Tasks

## 1. Check the deltas

- [x] 1.1 For each of the 20 MODIFIED requirements, compare the delta with the main spec: the header and every scenario are word for word the same, and the description is the original's opening behaviour with nothing reworded. Verify `openspec validate split-long-requirements --strict` passes.
  - Checked 31 October 2026 by script: all 20 MODIFIED headers and every scenario are the main specs' word for word; 16 descriptions are a prefix of the original, and the other four (the solution key, the screen, the header, validation) differ only by a cut-out clause and the full stop that now ends the sentence.
- [x] 1.2 For each ADDED requirement, check that its description says only what the long requirement it came from said, and that its new scenario holds for the code on `main` (the footers against `Wording` and `WordingOfOutcomes`, the keys against the reducers). Correct a scenario that overstates the code, never the requirement's meaning. Record anything doubtful here.
  - Checked 31 October 2026 against `main`: the rename, close-empty, adopt, delete and removal footers against `WordingOfOutcomes` and `Wording`; `/` clearing a message in `SessionsView`; the below-minimum words and comparison in `ClaudeCodeVersion`; the SESSION column's help; the worktrees filter matching the branch; `BatchFileArgument.IsSafe` refusing `%`; the placeholder problem's words; `OpenFileScript` starting the application without waiting; the picker's tab title from the 4.3 drive of config-tab-follow-ups. One scenario was restated while drafting (the dash-led prompt after `--`, whose parse was unverified). Nothing doubtful remains.
- [x] 1.3 Check that every sentence of the 20 long descriptions is in either the shortened MODIFIED description or one of the ADDED requirements, so nothing is lost.
  - Checked 31 October 2026 by script, word by word: every word of the 20 descriptions is in the shortened one or the spec's ADDED requirements, except where a sentence became a SHALL statement (`ends` and `is refused` to `SHALL end` and `SHALL be refused`, `copied` and `pruned` to `copy` and `prune`, `this way` to `as a plain folder`, `outside one the label stands alone` to `SHALL have its label stand alone`).

## 2. Gate and review

- [x] 2.1 Verify the pull request touches only `openspec\`, so CI takes its documentation-only path and `Version` stays as it is.
  - Checked 31 October 2026: the 12 changed files are all under `openspec/` and `.github\scripts\Test-DocumentationOnly.ps1 -Base origin/main -Head HEAD` answers True; `Version` is unchanged.
- [x] 2.2 The product owner reads the split requirements in the pull request. Record here.
  - Recorded 31 October 2026: the owner reviewed the split requirements in pull request #126 and asked for the change to be archived.

## Workflow follow-up

- Archive in the pull request that completes the change, then verify `openspec validate --specs --strict` passes on all 16 specs.
- Propose a separate change for session-listing's "The header shows the versions and the roots", which still describes the repository roots and a second settings row that config-tab moved to the Config tab.
