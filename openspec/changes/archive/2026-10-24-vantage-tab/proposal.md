# Proposal

## Why

The tab Vantage runs in looks like any other PowerShell tab, so among the coloured session tabs it is the one that is hardest to find. Windows Terminal lets a running program set its own tab title and background with escape sequences, and Vantage already knows which palette colours its session tabs use.

## What Changes

- When the console starts, Vantage titles its own tab "Vantage" and colours it green when no live Vantage-opened tab is green, otherwise the first free palette colour.
- Vantage registers its own tab (keyed "vantage", claimed by its own process) so later session tabs avoid its colour, and the registration goes stale with the process like any other.
- On quit, Vantage clears the title and resets the tab colour so the shell's tab looks as it did.
- A tab launched with an explicit `--tabColor` cannot be recoloured by escape sequences (a Windows Terminal rule), so a profile that sets a colour keeps it; the title still changes.

Out of scope: `vantage here` (it hands the tab to Claude at once); choosing the colour in configuration; multiple Claude accounts.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: a new requirement that Vantage's own tab is titled and coloured, and that its colour counts as in use for session tabs.

## Impact

- `Domain\TabColourSelection`: a preferred colour.
- `Actions\OwnTab`: claims the registration and chooses the colour.
- `Adapters\Terminal\TabEscapes`: the title and palette-entry sequences.
- `Program.cs`: writes the sequences at start and on quit. README.
