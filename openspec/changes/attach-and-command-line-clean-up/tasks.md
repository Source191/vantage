# Tasks

## 1. The colour reset at the terminal (before the code)

- [ ] 1.1 The product owner, from a PowerShell tab outside Claude Code, colours the tab as Vantage does (`"`e]4;264;rgb:16/c6/0c`e\" + "`e[2;263;264,|"` written with `Write-Host -NoNewline`), then writes `"`e]104;264`e\"`. Verify the tab returns to its profile's colour; if not, write `"`e]104`e\"` and verify again. Record the outcome here; design.md names the fallback if neither works.

## 2. The return hint

- [x] 2.1 Red: `WordingShould.ReturnHintIsOneShortLine` asserts `Wording.ReturnHint` is exactly "Ctrl+Z returns to Vantage." and no longer than 40 characters. Green: the new sentence. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` passes.
- [x] 2.2 Red: `TerminalEffectsShould.ShowTheHintBeforeTheAttachAndRemoveItAfter` runs `AttachToSession` with a `FakeTerminalConsole` and asserts the recorded order: hint shown, attach run, hint removed. Green: `ITerminalConsole` gains `ShowReturnHint` and `RemoveReturnHint`, `FakeTerminalConsole` records them, `TerminalEffects` takes the port instead of a `TextWriter`, and `Program.cs` hands it the `TerminalConsole`. Verify the Core suite passes.
- [x] 2.3 Red: `TerminalConsoleShould` asserts `ShowReturnHint` writes the hint followed by a newline and `RemoveReturnHint` writes `ESC[1A ESC[2K`. Green: the adapter, with the erase sequence named in `TabEscapes` or `ConsoleRecovery`. Verify `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` passes.

## 3. The quit colour

- [x] 3.1 Red: `TabEscapesShould` asserts `ResetBackground` is OSC 104 for entry 264 followed by DECAC, as 1.1 settled it, and `TerminalConsoleShould` asserts `ClearOwnTab` writes the title clear then that sequence. Green: `TabEscapes.ResetBackground`. Verify the Infrastructure suite passes.
  - Written with OSC 104 on entry 264 then DECAC, the design's first choice; revisit if 1.1 finds Windows Terminal ignores it.

## 4. Remove the agent-view key

- [x] 4.1 Red: in `SessionsViewShould` the `'a'` row of the key table goes and a fact asserts `a` produces no effect; `WordingOfKeysAndColumnsShould`'s expected lines lose "a Agent view"; `ScreenLoopShould`'s hand-off case uses `AttachToSession`; `TerminalEffectsShould.OpenTheNativeAgentView` and `ClaudeCliShould.OpenTheNativeAgentViewInteractively` are deleted. Green: delete `OpenAgentView`, its reducer case, the `TerminalEffects` case, `IClaudeCli.OpenAgentViewAsync` and `ClaudeCli`'s method, and the `a` line in `SessionsKeys`; the build names every other user. Verify both suites pass, including the reflection facts for keys and leaving effects.
- [x] 4.2 README: remove the `a` row from the keys table, the "The native agent view (a)" paragraph and the "Something needs Claude Code's own screen" entry's key, saying `claude agents` in a shell instead; reword the roadmap's pinning line without the key. Verify `grep -n "agent view" README.md` finds only the roadmap and the troubleshooting sentence.

## 5. The 2.1.310 facts

- [ ] 5.1 Red: `CommandLineShould` asserts `vantage here --help` describes `--prompt` with "waits at its prompt" and never mentions an agent list. Green: the description in `CommandLine.Here`; the wizard's prompt caption in `WizardControls` reads "Opening prompt (wraps; blank leaves the session waiting at its prompt)". Verify the Core suite passes.
- [ ] 5.2 Docs: README's "Opening prompt", "Attach in this tab", `vantage here` options and troubleshooting passages; CLAUDE.md's CLI facts (the `--bg -n` and attach sentences) with the heading's date and version moved to 2026-10-29 and 2.1.310. Verify `grep -rn "agent list" src README.md CLAUDE.md` finds nothing.

## 6. vantage here -d x

- [ ] 6.1 Red: `CommandLineShould.NameTheOptionLikeNameBeforeTheLibrarysOwnError` invokes `here -d x` and asserts standard error starts with `Wording.NameLooksLikeAnOption("-d")`, then holds the library's sentence naming `x`, the exit code is 1 and `StartHere` was never called. Green: `CommandLine.InvokeAsync` inspects the parse result for an option-like name before invoking, as design.md says. Verify the Core suite passes and the existing `--promt` fact still passes.

## 7. Version and checks

- [ ] 7.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one preview step above `main`'s (a minor raise: a key is removed). Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.
- [ ] 7.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal`. Verify exit 0, and that `drive-raw.txt` holds the erase sequence after Claude's screen.
- [ ] 7.3 The product owner installs the build with `tools\install-local.ps1`, attaches to a session three times with Ctrl+Z each time, attaches to a session that has never been sent a message, presses `a`, then quits. Verify Claude's prompt appeared for the empty session, `a` did nothing, the scrollback holds no hint after quitting, and the tab is its profile's colour; record here. If the erase removed a line of Claude's output, switch to the once-per-run fallback in design.md and record that instead.

## Workflow follow-up

- Archive the change in the same pull request, syncing the two delta specs into `openspec\specs`.
- The pull request changes attach and quit, so the README's "Manual checks for tab and attach changes" run in the same terminal session as 7.3.
