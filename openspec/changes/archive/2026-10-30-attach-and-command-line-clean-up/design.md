# Design

## Context

`TerminalEffects` writes `Wording.ReturnHint` to a `TextWriter` before `claude attach`, and `ScreenLoop.HandOverAsync` calls `ITerminalConsole.RecoverAfterChild` then `ShowOwnTab` when the child returns; nothing writes to the primary screen after that. Claude Code draws on the alternate screen, so when it exits the cursor is back on the line after the hint. Quitting calls `ITerminalConsole.ClearOwnTab`, which writes the title clear and `TabEscapes.ResetBackground`, today DECAC alone. `CommandLine.InvokeAsync` parses and invokes; the "looks like an option" check lives in the `here` action, which System.CommandLine never reaches when the parse has errors.

## Goals / Non-Goals

**Goals:**
- Nothing Vantage prints before a hand-off survives the return.
- Every sentence that describes an empty session matches Claude Code 2.1.310.
- The option-like check is heard whatever else is wrong on the command line.

**Non-Goals:**
- Showing Ctrl+Z inside Claude's own screen: Vantage cannot draw there.
- Changing what the attach tab does when the attach ends (session-tabs-and-delete).

## Decisions

- **The hint goes through the console port, and is erased after the child.** `ITerminalConsole` gains `ShowReturnHint` and `RemoveReturnHint`; `TerminalEffects` takes the port instead of a `TextWriter` and calls show, attach, remove, so `TerminalEffectsShould` asserts the order on `FakeTerminalConsole`. The adapter writes the hint and a newline, then `ESC[1A ESC[2K` (cursor up one line, erase it) through `ConsoleRecovery.WriteSequence`, which turns on escape processing. One short sentence that never wraps at 80 columns is what makes "up one line" exact.
  - *Rejected: print the hint once per run.* Leaves one line per run in the scrollback and loses the reminder on the second attach. It is the fallback if the final terminal check finds the erase takes a line of Claude's own output.
  - *Rejected: write the hint on the alternate screen.* Claude clears it on entry.
- **Quit resets palette entry 264 with OSC 104;264, then writes DECAC as today.** OSC 104 is the inverse of the OSC 4 that coloured the entry; DECAC stays so the tab's colour indices are the terminal's own again. The first task checks the sequence at the terminal, because nothing documents index 264; if Windows Terminal ignores it, the task tries a bare OSC 104 (reset every entry) and records which worked, and if neither does the design falls back to DECAC alone with the README saying the colour stays until the tab is reused.
- **The `a` key is removed outright, not hidden.** The effect record, reducer case, port method, adapter method, key line and spec requirement go, so the reflection facts and the build (a switch with a missing case) name every leftover. The README's pinning line on the roadmap keeps its sense (pinning waits for the CLI) without naming the key.
- **The option-like check runs on the parse result in `CommandLine.InvokeAsync`,** before `InvokeAsync` hands the errors to the library: when the `here` command's name token starts with `-` and no `--` precedes it, Vantage's sentence is written to the error writer first, and the library's own error and help follow. The action keeps its check for the no-error case, so both paths print the same sentence once.
  - *Rejected: a validator on the argument.* It cannot see the `--` token (the comment in `CommandLine.OptionLikeName` records why).
- **Wording for an empty session:** "the session waits at its prompt" replaces "opens on the agent list" in the `--prompt` description, the wizard caption and the README; the attach troubleshooting entry about a grey list is deleted rather than reworded.

## Risks / Trade-offs

- [The erase removes a line Claude printed on the primary screen on exit] → Claude 2.1.310 prints nothing there on detach (screenshot of the scrollback after five attaches shows only Vantage's hints); the final terminal check confirms, with the once-per-run fallback ready.
- [OSC 104 on an index above 255 is ignored] → checked first, with two fallbacks recorded in the task.
- [A user had `a` in muscle memory] → pressing it does nothing; the README's troubleshooting entry names `claude agents`.
