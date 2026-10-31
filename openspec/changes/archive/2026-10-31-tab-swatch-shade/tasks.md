# Tasks

## 1. Measure the fade (spike)

- [x] 1.1 Open one Vantage session tab in each palette colour, select the Vantage tab, and screenshot the tab strip in Windows Terminal 1.25 (dark theme). Read each unselected tab's fill and the tab row's background from the pixels, fit the rule (most likely a blend at a fixed alpha), and record the colours, the rule and the Windows Terminal version here. Verify the rule reproduces every measured shade within one step per channel.
  - Measured 30 October 2026, Windows Terminal 1.25.2733.0, its default dark theme on Windows 11 26100: a new window (`wt -w new --size 200,20`) of eleven tabs opened with `--tabColor` in each palette colour and a plain tab selected last, captured with `PrintWindow` (`PW_RENDERFULLCONTENT`, so the capture worked with the workstation locked), each tab's fill read as the most common pixel inside its UI Automation bounding box (about 3,900 of its 4,736 pixels).
  - Tab row #2E2E2E (the strip's background, wherever no tab is drawn). Unselected fills: Red #E74856 → #653539, Blue #0078D4 → #204460, Yellow #FFB900 → #6D5720, Purple #8764B8 → #483E57, Green #16C60C → #265B23, Orange #F7630C → #6A3D23, Teal #00B7C3 → #20575A, Pink #E3008C → #64204A, Brown #8E562E → #4A392D, Lime #9ACD32 → #4E5D2F, Grey #7A7574 → #444343.
  - Rule: each channel is the tab colour at alpha 77 of 255 (Windows Terminal's own 30 % for a deselected coloured tab) over the tab row, `(colour × 77 + 46 × 178) / 255` in integer arithmetic. It gives 28 of the 33 channels exactly and the other five one step high, so no table of shades is needed.

## 2. The shade (Domain)

- [x] 2.1 Red: a `TabShade` fact per palette colour asserts the measured shade. Green: the rule beside `TabPalette`. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 3. The screen and the words

- [x] 3.1 `SessionsScreenDefinition.CellPaint` paints the TAB block with the shade, on the selected row too. Verify with a headless capture that the block's colour is the shade for the session's tab colour.
  - Checked 30 October 2026 with the Debug build under `drive`: a prompt-less throwaway `claude --bg` session, filtered to and opened with `o` (a real tab, so its registration was live), drew its block as `38;2;106;62;35` (Orange's shade) on the selected row, and the owner's three open tabs as the Red, Blue and Yellow shades; no block in a palette colour's bright value appeared. The session was stopped and removed.
- [x] 3.2 Red: `WordingOfKeysAndColumnsShould` asserts the TAB column's help says the block matches the tab as it looks from Vantage. Green: the words; README's TAB row and CLAUDE.md's Windows Terminal fact (the fade, how and when it was measured). Verify the README matches `Wording`.

## 4. Version and checks

- [x] 4.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate tab-swatch-shade --strict` pass.
- [x] 4.2 Run the hand-off check and the smoke check against the packed tool. Verify both exit 0.
  - 30 October 2026, 0.18.0-preview.10 (raised to preview.11 on 31 October, when `main` reached preview.10): the hand-off check exited 0 (Claude's screen in the capture, the throwaway session removed) and the smoke check of the package installed into a temporary `--tool-path`, against the linked worktree `vantage-v-test-prs`, exited 0.
- [x] 4.3 The product owner compares each TAB block with its tab in the strip, on the selected row and others. Record here.
  - 31 October 2026, 0.18.0-preview.10-local: the owner found the blocks match the tabs.

## Workflow follow-up

- Implement after detail-pane, once where-column and tabs-and-key-bar have archived. Archive in the pull request that completes it.
