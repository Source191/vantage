# ReadmeScene

Makes the README's screenshot (`assets\readme\sessions.png`) and animation (`assets\readme\new-session.gif`) from the real console screen, over a made-up scene. Run it through `tools\New-ReadmeScreenshot.ps1`, which builds the scene's repositories, runs this program in a Windows Terminal window of its own, captures it and cleans up; see CONTRIBUTING for when to.

## What it is

`SceneHost` composes `ConsoleScreen` and `ScreenLoop` as the tool project's `ConsoleComposition` does, with the scene's parts in place of the machine's:

- **Claude Code, gh and az:** `SceneProcessRunner` answers `claude agents --json --all`, `--version` and `--bg -n` from `SceneListing`, and `gh pr list` and `az repos pr list` from `ScenePullRequests`, all read from `scene.json` through the real `ClaudeCli` and pull-request adapters. Any other Claude, gh or az command fails; git, `wt` and `pwsh` run for real, and every `wt -w 0` is sent to the scene's named window (`readme-scene`) so no tab lands in another window.
- **State, transcripts, crash file:** the real adapters, pointed at the scene folder (`state` for `%LOCALAPPDATA%\Torni`, `claude` for `~\.claude`), never at yours. The host writes an empty transcript per session, which is all Enter checks for.
- **Processes:** `SceneProcessRegistry` answers for the scene's made-up Claude processes and never ends one; the tab shells are real and go to the real registry.
- **Tabs:** the real `WindowsTerminal` adapter and `TabScripts`, with this program standing in for both Claude Code (`attach` shows a placeholder line) and Torni (`claim-tab` stamps the registration in the scene's state, as Torni's own does), so tab titles, colours, profiles and the TAB swatches are exactly what Torni makes.
- **Keys:** `ConsoleKeys` writes key events into the console's own input (`WriteConsoleInputW`), which Terminal.Gui reads as typed keys, with no window focus needed. `SceneKeys` opens each tabbed session's tab through the filter and Enter and selects the waiting session; `new-session.keys` is the animation, in the `KeyScript` format.
- **Capture:** `WindowCapture` uses `PrintWindow` with `PW_RENDERFULLCONTENT`; `FrameRecorder` takes a frame every 100 ms (`FrameSchedule`) while a recorded script plays.
- **Never touched:** your own settings and state, `~\.claude`, your sessions, the Windows Terminal fragment (read, never written) and the network (the update check is off).

## Changing the scene

- **What the screenshot shows:** edit `scene.json`. Only Northwind.* repositories with `northwind-demo` remotes, and Torni itself at `github.com/Source191/torni`, are accepted (`SceneFile`); times are minutes or days before now, so the AGE column reads naturally on any day.
- **How the repositories look:** `Set-WorktreeHistory` in the script gives each worktree its commits, pushed or merged state and uncommitted changes, by the session's state.
- **The animation:** edit `new-session.keys`. Each line waits its delay in milliseconds, then types text, presses a named key (`NamedKey`), returns to the console's tab (`focus`) or writes a mark file the script waits for. The wizard's fields are left with Enter, not Tab.

## Untested wiring

`SceneHost`, `Program`, `KeyPlayer`, `ConsoleKeys`, `SceneWindow`, `WindowCapture`, `FrameRecorder` and `NativeMethods` are untested wiring, as the tool project's composition root is: they compose the console or call Win32 directly. Everything that decides what the scene says (`SceneFile`, `SceneListing`, `ScenePullRequests`, `SceneProcessRunner`, `SceneProcessRegistry`, `SceneKeys`, `KeyScript`, `FrameSchedule`) is tested in `tools\ReadmeScene.Tests`. A constructor change in `ConsoleComposition`'s parts breaks this project's build in CI; a new column the scene does not fill shows up the next time the images are made.

- **`AllowUnsafeBlocks`:** the `[LibraryImport]` source generator behind `NativeMethods` refuses to run without it; the project writes no `unsafe` code of its own.
- **`InvariantGlobalization`:** the console runs in the invariant globalisation mode it ships in.
- **`System.Drawing.Common`:** encodes the captures as PNG; Windows only, like the rest of the tool.
