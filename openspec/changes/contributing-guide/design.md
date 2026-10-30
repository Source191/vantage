# Design

## Context

See proposal.md for the motivation. The README is packed into the tool as its nuget.org readme, so a relative link from it to `CONTRIBUTING.md` would break there; the project READMEs already link each other through absolute `https://github.com/Source191/vantage/blob/main/...` URLs. CLAUDE.md is the rulebook a Claude Code session reads first and holds the verified CLI, Terminal.Gui, Windows Terminal and process facts; several `.github` files point readers at README sections by name.

## Goals / Non-Goals

**Goals:**
- One document a new contributor can read top to bottom and then make a change the way this repository expects.
- OpenSpec explained as a sequence of stages with what each produces, who does it and how to tell it is done, plus the reasons the specs and the archive are kept.
- The README reads as a user guide only.

**Non-Goals:**
- Duplicating CLAUDE.md's verified facts or every code rule; CONTRIBUTING carries the shape and links the detail.

## Decisions

- **One file, maintainer section last.** The owner-only material (releases, publishing, secrets, tokens, rulesets, the nightly contract check) goes in a final "Releasing and maintaining" section rather than a second document, so there is one place to look and a contributor can stop before it. Alternative considered: `docs\maintaining.md`; rejected by the owner to keep one file.
- **Summarise the code rules, link CLAUDE.md.** CONTRIBUTING gives the layers, the TDD loop, sociable tests and fakes, text values, `Wording`, the privacy rule for diagnostics, and the four pieces of a new key, each in a line or two, and names CLAUDE.md as the authority. Alternative: moving the rules into CONTRIBUTING and slimming CLAUDE.md; rejected as a larger change that would also alter what sessions load.
- **Issue first for outside contributors.** Anything beyond a small fix starts with an issue so the change can be agreed before an OpenSpec proposal is written; the owner's approval is already enforced by CODEOWNERS and the "Owner approves main" ruleset. Contributions are under the repository's Apache 2.0 licence (inbound equals outbound, section 5), with no CLA.
- **Move text, do not rewrite it.** The CI, release and version sections move largely as written, re-headed to fit the new document; only the OpenSpec section is substantially new.
- **Absolute link from the README.** The README's new "Contributing" section links `https://github.com/Source191/vantage/blob/main/CONTRIBUTING.md`, matching how it already links images and project READMEs.

## Risks / Trade-offs

- [A link or a "see README" pointer left behind] → search the repository (outside `openspec\changes\archive`) for the removed headings and anchors and for "see README" before finishing.
- [The nuget.org readme keeps the old sections until the next release] → acceptable; the next tool release repacks the README.
- [CLAUDE.md and CONTRIBUTING drift apart] → CONTRIBUTING states CLAUDE.md wins and keeps only summaries that change rarely.
