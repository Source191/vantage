# Design

## Context

`release.yml` creates a GitHub Release whenever a push to `main` changes `Version`; `Assert-VersionBumped.ps1` fails a pull request that changes the tool without raising `Version` above `main`'s, comparing semantic versions. `main` is at `0.17.0`.

## Goals / Non-Goals

**Goals:**
- Every release from now on is a preview until the owner chooses otherwise.

**Non-Goals:**
- Deleting the v0.17.0 tag or release: links to it keep working, and it can be marked a full release again.

## Decisions

- **`0.18.0-preview.1`, not `0.17.1-preview.1`.** The security work and what follows are new behaviour, so they build towards a minor; the owner chose it.
- **Mark, do not delete, the v0.17.0 GitHub Release.** Marking it a pre-release is reversible and keeps the tag.

## Risks / Trade-offs

- [Someone installed 0.17.0 from the GitHub Release's nupkg] → `U` and the nuget.org check never offer previews, so they stay on 0.17.0 until the next full release; installing a preview by hand still works.
