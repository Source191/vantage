# Proposal

## Why

The first time the owner opens the Worktrees tab (31 October 2026, 0.19.0-preview.2) it shows "Scanning worktrees" over an empty table for several seconds, because the scan only starts when the tab is first selected. Every worktrees scan also runs its own `claude agents --json`, although the Sessions tab lists the same sessions every five seconds. The archived tabs-and-key-bar design listed "warming the worktrees scan before its tab is opened" as a non-goal without a reason; this change reverses that.

## What Changes

- **Scan at start-up:** the console starts one worktrees scan as it opens, and the scan waits for the Sessions tab's first listing (succeeded or failed) before it reads anything, so the two never compete for the first seconds. Opening the Worktrees tab after the scan has landed shows its rows at once; opening it sooner shows the waiting spinner as today, over the same scan, without starting a second one.
- **Quiet rescan on show:** each time the Worktrees tab is shown it rescans without the spinner, so rows read minutes earlier refresh within seconds.
- **Timed rescans unchanged:** the 15-second rescan still runs only while the Worktrees tab is shown; a hidden Worktrees tab costs nothing after the start-up scan.
- **One scan at a time:** neither showing the tab nor the timer starts a scan while one is running. A first scan can take around forty seconds on fifty repositories (README), so today's timer already stacks scans on a large roots folder, and a rescan on every show would stack more.
- **One listing, not two:** the worktrees scan reads the sessions from the Sessions tab's latest listing instead of running `claude agents --json` itself, which removes a duplicate child process from every worktrees scan. A listing is at most five seconds old when a scan reads it.
- **Version:** one preview step above `main`'s at the time of merge.

Out of scope: rescanning the worktrees while their tab is hidden, sharing anything else between the tabs (the pull request catalogue is already shared), changing the Sessions tab's five-second listing, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `console-layout`: "Sessions and worktrees are two tabs of one screen" replaces its "Worktrees not yet scanned" scenario with the start-up scan and the rescan on show.
- `worktree-management`: ADDED "The worktrees are scanned as the console opens" and "A worktrees scan reuses the sessions listing"; "Scanning is visible" shows the spinner only while the tab is shown before the first scan lands, keeps it off the rescan on show as well as the timed rescans, and names the status line under the versions (where status-line-under-settings put the working text) in place of the footer.

`detail-pane`'s "The first worktrees scan" scenario ("first shown and its scan has not finished") stays true of the start-up scan and needs no delta.

## Impact

- **Application:** a new holder of the latest sessions listing, written by the Sessions tab's load and read by the worktrees scan, which waits for its first listing.
- **Tui:** `ConsoleScreen` starts the worktrees load once as the frame opens, triggers it on every show of the Worktrees tab, drops `worktreesScanned`, and its timer refreshes the worktrees only while their tab is active.
- **Program.cs:** `LoadAsync` lists through the holder; `ScanAsync` reads from it instead of calling `claude.ListSessionsAsync`.
- **Start-up cost:** the start-up scan runs git in every repository and worktree under the roots, and asks `gh` or `az` for stale pull requests, even when the Worktrees tab is never opened.
- **Tests:** the holder's facts in Core.Tests; the smoke run and `DiagnosticsPrivacyShould` re-checked.
- **Docs:** README (the Worktrees tab), CLAUDE.md (the console paragraph that says a tab asks only while shown), the specs.

## Order

Archived after status-line-under-settings (merged in #129, not yet archived), which adds requirements to `console-layout` beside the one this change modifies and moved the working text this change's "Scanning is visible" now names.
