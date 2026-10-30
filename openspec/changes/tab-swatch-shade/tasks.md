# Tasks

## 1. Measure the fade (spike)

- [ ] 1.1 Open one Vantage session tab in each palette colour, select the Vantage tab, and screenshot the tab strip in Windows Terminal 1.25 (dark theme). Read each unselected tab's fill and the tab row's background from the pixels, fit the rule (most likely a blend at a fixed alpha), and record the colours, the rule and the Windows Terminal version here. Verify the rule reproduces every measured shade within one step per channel.

## 2. The shade (Domain)

- [ ] 2.1 Red: a `TabShade` fact per palette colour asserts the measured shade. Green: the rule beside `TabPalette`. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 3. The screen and the words

- [ ] 3.1 `SessionsScreenDefinition.CellPaint` paints the TAB block with the shade, on the selected row too. Verify with a headless capture that the block's colour is the shade for the session's tab colour.
- [ ] 3.2 Red: `WordingOfKeysAndColumnsShould` asserts the TAB column's help says the block matches the tab as it looks from Vantage. Green: the words; README's TAB row and CLAUDE.md's Windows Terminal fact (the fade, how and when it was measured). Verify the README matches `Wording`.

## 4. Version and checks

- [ ] 4.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate tab-swatch-shade --strict` pass.
- [ ] 4.2 Run the hand-off check and the smoke check against the packed tool. Verify both exit 0.
- [ ] 4.3 The product owner compares each TAB block with its tab in the strip, on the selected row and others. Record here.

## Workflow follow-up

- Implement after detail-pane, once where-column and tabs-and-key-bar have archived. Archive in the pull request that completes it.
