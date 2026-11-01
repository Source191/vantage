# Design

## Context

`TabKeys.TryFor` answers the tab effects for Left, Right and the digits, and every view's reducer asks it first. `Wording.TabKeyHelp` lists the help-only tab keys, and the headless smoke script finds the key for a tab through that list (`SmokeScript.TabKey` looks up the line "the Config tab"). The README is packed into the nupkg as its readme, and `DocumentedMinimumShould` reads it for `2.1.309 or later`.

## Goals / Non-Goals

**Goals:**
- Digits fall through `TabKeys` like any other unbound key; nothing else about tab switching changes.
- The smoke script keeps reaching every tab, with the keys a person now uses.
- A README someone can read in a couple of minutes, with every claim in it still true.

**Non-Goals:**
- Binding the digits to anything else.
- Changing what `vantage here` prints.

## Decisions

- **`TabKeys` loses its three digit arms, and `SelectTab` stays.** Esc and a click on a tab's title still raise `SelectTab`, so the record and its routing are unchanged; only the summaries that mention 1, 2 and 3 change.
- **The smoke script steps with Left and Right.** `TabKey(keys, tab)` becomes a function of the tab shown and the tab wanted that repeats the help's → or ← as many times as the tabs are apart in `ConsoleTab`'s order, and `Sequence` learns the terminal sequences for ← and → (`ESC [ D` and `ESC [ C`). Alternative considered: a click on the tab's title, which the headless terminal cannot send. Every step that switches tab starts after Enter has committed any filter, so the arrows reach `TabKeys` rather than a filter field.
- **README: delete, not move.** The owner chose deleting the detail over a `docs\user-guide.md`; the `?` help, tooltips, key bar and specs describe every key and column. The README keeps prerequisites, install, a quick start, the everyday keys, a short notifications section (the needs-input-notification spec still asks for it) and links, including the `!` key for problem reports and SECURITY.md for privacy.
- **Changelog: amend the #117 line.** The digits only ever appeared under Unreleased, so the Added line drops them and names this pull request beside #117, rather than a Removed line for a key no release had.

## Risks / Trade-offs

- [A reader who wants the full detail no longer finds it in the README] → `?` on every tab lists every key, column and detail line; the README says so near the top of its key table.
- [The smoke script's arrow sequences differ from what Windows Terminal sends] → `ESC [ C` and `ESC [ D` are what the headless terminal's pseudo-console delivers for Right and Left, the same keys `TabKeys` already handles; CI's smoke check proves it on the next run.
