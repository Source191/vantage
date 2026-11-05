# Design

## Context

See proposal.md for why. Three facts shape the screenshot approach:

- **Claude Code is found before the PATH:** `ClaudeExecutable.Locate` takes the native `~\.local\bin\claude.exe` first, so a stub `claude.exe` on the PATH is never run on a machine with Claude Code installed.
- **State and transcripts are fixed folders:** the state folder is `Environment.GetFolderPath(LocalApplicationData)\Torni`, which ignores `LOCALAPPDATA` (`checks.md`), and `ClaudeTranscripts.ForCurrentUser` reads `~\.claude\projects`, which Enter and resume check before they act. Running the shipped `torni.exe` for a demo would read and write the owner's real state.
- **The pieces are public:** `ConsoleScreen`, `ScreenLoop`, `ClaudeCli`, `GitCli`, `TabScripts`, `WindowsTerminal`, the PR adapters and the TestDoubles fakes (`FakeProcessRunner`, `FakeProcessRegistry`, `FakeTranscripts`, `InMemoryState`) are all public, so another program can compose the real screen as `ConsoleComposition` does.

## Goals / Non-Goals

**Goals:**

- One screenshot that shows as much as one screen can: every session state, a background and an interactive session, an automated run in the counts, GitHub and Azure DevOps pull requests in several states, TAB swatches matching coloured tabs in Windows Terminal's tab strip, the detail pane on a waiting row, the settings row and the key bar.
- A short GIF of starting a session with `n`: the wizard, F5, the new coloured tab, and the new row back in Torni.
- Regenerating both is one command, deterministic, and safe on the owner's everyday machine.

**Non-Goals:**

- Testing Torni: the scene host is a picture-maker, not a check; the smoke and hand-off checks stay as they are.
- Showing the Worktrees or Config tabs: the guide describes them in words.

## Decisions

### README outline

The README becomes a front page in this order (draft wording, to be polished in the PR):

1. **Banner, credit and support button:** the banner unchanged; under it the official Buy Me a Coffee button (`https://cdn.buymeacoffee.com/buttons/v2/default-orange.png`, about 40 px high, linking to `https://buymeacoffee.com/simonvane`) in place of today's shields.io sponsor badge, and a "Built by Simon Vane" badge (shields.io, LinkedIn logo) linking to `https://www.linkedin.com/in/simonvane/`. A badge for the credit because it sits in the first screenful without pushing the problem down.
2. **The problem:** "Five Claude Code sessions. A dozen terminal tabs. One of them finished ten minutes ago, another has been waiting for your permission since lunch, and you cannot remember which tab is which." Then one sentence: "Torni is mission control for Claude Code on Windows: one tab that lists every session on the machine, rings when one needs you, and opens any of them in its own coloured tab."
3. **The screenshot:** `assets\readme\sessions.png`, with alt text describing what it shows.
4. **What it does:** the six feature bullets trimmed to five, each one line.
5. **Why I built it:** the current paragraph, shortened to three or four sentences, keeping Orca and Kepler, and signed "Built by [Simon Vane](https://www.linkedin.com/in/simonvane/)" so the credit is next to the story as well as at the top.
6. **Support Torni:** directly after the story while the reader is warmest. The "gaps between rate limits" line, then a specific ask that says what the money buys: "If Torni saves you ten minutes a day, a coffee now and then keeps it moving. Every one goes towards the Claude plan it is built with." Then the official button, a line that a small monthly membership is there for regular users, and the QR code.
7. **Install:** `winget install Source191.Torni`, then the Claude Code installer lines, then `torni`; one sentence on what winget brings. Without winget and previews become one line linking to the guide.
8. **Getting started:** five short steps, the GIF beside the first, then a one-line ask with the official button again ("Got your first session running? Buy me a sliver of a Claude Max plan."), because people give just after they have felt the value, not before they have seen the tool:
   - **Create a session:** `n`, a repository, a name, an optional prompt, F5; it gets its own worktree and coloured tab.
   - **See your sessions:** the Sessions tab, what the states and the amber rows mean, Enter to switch to a session's tab.
   - **Run several:** `n` again for each piece of work, each in its own worktree; `torni here` for a folder you are already in.
   - **Close the terminal, keep the sessions:** sessions run under Claude Code's supervisor, so closing Windows Terminal stops nothing; start Torni again and every session is still listed, Enter reattaches; after a reboot they show stopped and `R` resumes them.
   - **Use the Torni profiles:** `torni setup-terminal` adds a "Torni" profile (start Torni from Windows Terminal's new-tab menu, or make it the startup profile) and a "Torni session" profile for session tabs, each with its own bell, and turns on Claude Code's terminal bell.
9. **Learn more:** the user guide first, then the existing in-tool help, changelog, privacy, security and contributing lines.

### Making support easy to find

- **GitHub's Sponsor button:** `.github/FUNDING.yml` with `buy_me_a_coffee: simonvane` makes GitHub show a Sponsor button in the repository header on every page, which no README placement can match. One line, no workflow.
- **The official button over a badge:** a shields.io badge looks like the status badges readers skim past; the official button is bigger, coloured and recognised, so it reads as an action and its destination is trusted. The "sliver of a Claude Max plan" joke stays as the text beside it, not on it.
- **Three places, no more:** the top button, the Support section after the story and the ask at the end of getting started. More would read as nagging.
- **Monthly memberships:** turned on in the Buy Me a Coffee account by the owner (a one-off step in the pull request description, not in any standing document), so the README's mention of them is true on the day it merges.

The keys table, notifications detail, without-winget, previews, upgrade and uninstall paragraphs move to `docs\user-guide.md`, which is organised as: installing (winget, without winget, previews, upgrading, uninstalling), the console (tabs, columns, detail pane, status line), sessions (start, open, stop, delete, resume, rename, adopt, `torni here`), worktrees, configuration, notifications and profiles, keys (the full table), privacy, and troubleshooting (the problem report, `last-crash.log`). The guide is written from the console's own `Wording` (key and column help), not invented, and links to nothing under `openspec`.

Images are referenced by their raw GitHub URLs on `main`, as the banner is, so they also render wherever the README is shown outside GitHub.

### The scene host, not the shipped tool

`tools\ReadmeScene` is a .NET 10 console project that composes Torni's real `ConsoleScreen` and `ScreenLoop` from the shipped `Core`, `Infrastructure` and `Tui` libraries, with these parts swapped:

- **Claude Code:** the real `ClaudeCli` over a `SceneProcessRunner` that answers `claude agents --json`, `--version` and `--bg -n` from the scene, and adds a new session to the listing when the wizard starts one. Everything else is refused with an exit code, so nothing real is ever run as Claude.
- **Pull requests:** the real `GitHubPullRequests` and `AzureDevOpsPullRequests` over the same runner, answering `gh pr list` and `az repos pr list` with the scene's pull requests for each repository's made-up remote. Nothing reaches GitHub or Azure DevOps.
- **git:** real git over the demo repositories the script builds, so branches, worktrees, health and the detail pane are Torni's own reading of real checkouts.
- **State, transcripts and processes:** the real `JsonFileStore`, `ClaudeTranscripts` and `CrashFile`, pointed at the scene folder (`state` and `claude` in place of `%LOCALAPPDATA%\Torni` and `~\.claude`), so tab registrations are made and claimed exactly as Torni makes them; the host writes an empty transcript per session, which is all Enter checks for. `SceneProcessRegistry` answers for the scene's made-up Claude processes and never ends one, and hands the real tab shells to the real `ProcessRegistry`. (The first plan was `InMemoryState` and the TestDoubles fakes; real adapters on a scene folder let the tabs claim themselves through `claim-tab`, so the TAB swatches are Torni's own.)
- **Windows Terminal:** the real `WindowsTerminal` adapter and `TabScripts`, so each tab's title, colour and profile are exactly what Torni would open. `TabScripts` is given `readme-scene.exe` as both the Claude and the Torni executable: `readme-scene attach <id>` shows a one-line placeholder until the scene closes, and `readme-scene claim-tab` stamps the registration in the scene's state with `TabClaimer`. Every `wt -w 0` the adapter runs is sent to the scene's named window (`-w readme-scene`), because a window opened while someone works elsewhere is not the most recently used one, and the first runs put tabs in the owner's own window. The fragment is read (does it exist?) and never written.
- **Updates and diagnostics:** the update check off and no log, so the run makes no network call and writes no file outside the scene folder.

**Why this over the alternatives:** a stub `claude.exe` on the PATH loses to the real one (Context); a separate Windows account would work but needs a second sign-in, and a managed machine may forbid it; hidden overrides in the product would mean a version raise and new surface to secure for a picture. The cost of the host is a second copy of the wiring, which drifts when `ConsoleComposition` changes; because the project is in the solution, a changed constructor breaks the build in CI rather than the next screenshot.

### The scene file

`tools\ReadmeScene\scene.json` holds everything shown: the demo repositories (name, remote URL, branches and commits the script makes), the sessions (name, kind, state, waiting reason, repository and worktree, start time as minutes before now, tab colour or none), the automated run, and the pull requests per branch (number, host, state, draft). Times are relative so the AGE column reads naturally on any day. The repositories are Northwind.Orders, Northwind.Billing, Northwind.Inventory, Northwind.Portal and `torni` itself (with `github.com/Source191/torni` as its remote, the one real name allowed). Remotes for the rest use the made-up `northwind-demo` GitHub owner and `dev.azure.com/northwind-demo`; nothing is fetched from them.

The sessions cover: working, waiting (selected, so the detail pane shows its reason), idle, done, failed, stopped and starting; one interactive session; GitHub open, draft and merged, Azure DevOps active and completed pull requests.

### The script

`tools\New-ReadmeScreenshot.ps1` (owner-run):

1. Closes a scene left running, deletes the scene folder (only one holding its `.readme-scene` marker), publishes `tools\ReadmeScene` into its `bin`, and checks for `wt.exe` and git; ffmpeg is checked only for the animation, which is skipped with a warning naming `winget install Gyan.FFmpeg` when it is missing.
2. Creates the scene folder (default `C:\ReadmeDemo`, `-SceneRoot` to change it) with `me\source\repos\<repository>` for each repository: `git init`, made-up author `Demo <demo@example.com>`, a few commits, the scene's branches and worktrees, the remote URL set but never fetched. The host is given `C:\ReadmeDemo\me` as home, so folder labels read `~\source\repos\...` and no real user name appears anywhere on screen.
3. Opens the scene's named window (`wt -w readme-scene --size 140,30`, which leaves less empty table than 160 x 40) with the cleared environment (`NO_COLOR`, `CLAUDECODE`, `CLAUDE_PID`, `CLAUDE_CODE_*` removed) running `readme-scene run`. The host titles its tab with a unique token to find its window handle, then plays `SceneKeys`: for each tabbed session, `/`, its name, Enter, Enter (which opens the tab), `wt focus-tab --target 0` back to Torni and Esc; then it selects the waiting row and writes `ready.mark`.
4. Captures the window with `PrintWindow` and `PW_RENDERFULLCONTENT` (works with the workstation locked, per `windows.md`) into `assets\readme\sessions.png`, done by `readme-scene capture <scene-root> <file>`.
5. For the GIF, a second run (`run ... --frames <folder> new-session.keys`) opens the tabs again, then plays the animation's keys while `FrameRecorder` saves a frame every 100 ms; ffmpeg turns them into `assets\readme\new-session.gif` at 960 pixels wide with a generated palette, kept under 2 MB. Keys are key events written into the console's own input (`WriteConsoleInputW`): `ConsoleScreen` creates its Terminal.Gui application inside `Run`, so `NewKeyDownEvent` cannot be reached from outside (spike 1.2). The wizard's fields are left with Enter, its own route between them; Tab stops at the selectors.
6. Closes the scene by writing `close.mark`: every placeholder exits 0, so Windows Terminal closes its tab, and the console quits with `q`, which closes the last tab and the window without the close-all prompt a `WindowPattern.Close` raises. Then it deletes the scene folder, leaving only the two images changed.

### Testing

As with `HeadlessTerminal`, the pure parts are unit tested in `tools\ReadmeScene.Tests`: the scene file's parsing and its relative times, the `SceneProcessRunner`'s answers (the listing JSON, the `gh` and `az` JSON, a started session appearing in the next listing, refusals), the scene's process table, the start-up keys, the key script's parsing, and the frame schedule. The composition, the native capture and the script are untested wiring, which `CLAUDE.md` already allows only in the tool project; this change records the same exception for `tools\ReadmeScene` in its README, as `HeadlessTerminal`'s README does for its pseudo-console.

### The notifications requirement

`needs-input-notification` requires the README to have the notifications section. The detail moves to the guide, and the README's getting started keeps `torni setup-terminal` as the one step for profiles and both bells, so the requirement is reworded rather than dropped (see the spec delta).

## Risks / Trade-offs

- [The host's wiring drifts from `ConsoleComposition`] → it builds in CI with the solution, so a changed constructor fails the pull request that changed it; behaviour drift (a new column the scene does not fill) shows up the next time the screenshot is made.
- [`wt -w 0` opens the session tabs in another window] → it did, in the owner's own window, while the owner worked elsewhere (spike 1.1 passed only with the new window in front); the scene's runner now rewrites `-w 0` to the scene's window name, with no product change.
- [Keys arrive faster than the console takes them] → the scripts leave half a second or more between steps, and the start-up keys wait 3.5 seconds for each tab to open.
- [The screenshot shows the owner's Windows Terminal theme and font] → the script opens the window with the default profile's settings; the README alt text does not depend on them. Using a fixed demo profile is left to a later change if it matters.
- [A made-up remote owner later exists for real] → nothing is fetched, and only the repository names and PR numbers appear on screen.
- [The GIF grows large] → 140 x 30 scaled to 960 pixels wide, at 10 frames a second for about 20 seconds, with a 128-colour generated palette; the task checks the size.

## Migration Plan

Docs and tools only: merge after #206. Nothing to roll back beyond reverting the commit.

## Open Questions

- The final wording of the opening and of the guide's sections, settled in the pull request review.
