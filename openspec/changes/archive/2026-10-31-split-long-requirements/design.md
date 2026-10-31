# Design

## Context

See proposal.md, Why. A MODIFIED requirement replaces the main spec's whole block at archive, and both `openspec validate` and archive refuse a MODIFIED block that drops a scenario the main spec still has. So a long requirement cannot simply be cut into pieces that share out its scenarios.

## Goals / Non-Goals

**Goals:**
- Every requirement in `openspec\specs` at 500 characters or fewer, so `openspec validate --specs --strict` passes on all 16 specs and can be used as a gate.

**Non-Goals:**
- Correcting a requirement that no longer matches the code (see Risks); that changes the contract and needs its own change.
- Moving existing scenarios between requirements.

## Decisions

- **Keep the name and every scenario; cut the description to its first behaviour.** Each long requirement is a MODIFIED block with its header and all its scenarios as they are, and a description of its opening behaviour in its original words. Alternative: REMOVED plus ADDED under new names, sharing the scenarios out; rejected because every requirement name that archived changes, the README or a reviewer refers to would change, and REMOVED reads as dropped behaviour.
- **Each behaviour cut out is an ADDED requirement in the same spec,** in the original words as far as a sentence allows, with at least one new scenario. The new scenarios state what the code does today, checked against `Wording` and the handlers while drafting (the rename, close-empty, adopt and removal footers, the safe-character rule for `az`, the custom editor's placeholder problem). Where an old scenario already covers a split-off behaviour, it stays where it is and the new scenario adds a different case.
- **New names state the behaviour,** for example "Rename moves the session's tab" or "A failed rename keeps the old name", so the list of requirements reads as a list of behaviours.
- **The split happens in the deltas; tasks only check and archive.** The deltas are the whole change; nothing else in the repository moves.

## Risks / Trade-offs

- [A new scenario could overstate the code] → each new scenario names behaviour already in the requirement text it came from, and footers are given as what they say rather than quoted, except where `Wording` was read.
- [A MODIFIED block keeps scenarios whose behaviour now lives in an ADDED requirement] → accepted; the instruction for splitting asks for exactly this, and archive refuses anything else.
- **Found while splitting, left for a change of its own:** session-listing's "The header shows the versions and the roots" still says the settings rows show the repository roots and the worktree root, and its "Released versions" and "Default worktree root" scenarios describe a second settings row. Since config-tab the console has one settings row (version, memory and Claude), and the roots are only on the Config tab. Splitting keeps that wording as it is.
