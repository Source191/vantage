# Proposal

## Why

Vantage is free, and the owner now has a Buy Me a Coffee page (https://buymeacoffee.com/simonvane, where "coffee" is replaced by "claude max plan") with nowhere pointing at it. The page's current cover is a cropped, blurred close-up of the icon; it needs a proper header, and the README and the repository's link preview would benefit from the same artwork.

## What Changes

- `vantage --help` gains a second line in its description: `Buy me a sliver of a Claude Max plan: https://buymeacoffee.com/simonvane`. The URL is held once, in `Wording`, and `CommandLine` reads it.
- `Version` rises to `0.17.0-preview.3`.
- The README gains a Buy Me a Coffee badge under the title, a "Support Vantage" section before "Roadmap and known limits" (rate-limits wording: "Vantage is built in the gaps between rate limits. Help close the gaps: buy me a sliver of a Claude Max plan.") with the link and a small QR code, and a Contents entry for it.
- `.github\FUNDING.yml` names `buy_me_a_coffee: simonvane`, so GitHub shows a Sponsor button on the repository once it reaches `main`.
- A new script, `tools\New-Header.ps1`, draws a header from the existing icon mark (the V of two branches merging into a node) beside the name and tagline, and writes three committed PNGs under `assets\header\`: a 3200 x 800 Buy Me a Coffee cover (text kept clear of the bottom-centre strip that the page's cards overlap), a README banner that replaces the 128 px icon at the top of the README, and a 1280 x 640 GitHub social preview. The QR code is committed at about 160 px under `assets\support\`.

Out of scope: the Keys help dialog, which already loses its last three lines (the TAB column, a blank line and the Claude Code path) on a 40-row terminal, so a line added at its foot would never be seen; that is a bug for its own change. Also out of scope: a new icon (the header reuses the mark as drawn), uploading the cover and social preview (the owner does that by hand on Buy Me a Coffee and under the repository's Settings), and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `command-line`: "Help is printed on request" also says where to support Vantage.

## Impact

- `src\Source191.Vantage.Core\Application\CommandLine.cs` and `Wording` (the support line); `CommandLineShould` and `WordingShould`.
- `src\Source191.Vantage\Source191.Vantage.csproj`: `Version`.
- `README.md`, `.github\FUNDING.yml`, `tools\New-Header.ps1`, `assets\header\*.png`, `assets\support\buy-me-a-coffee-qr.png`.
- README images use absolute `raw.githubusercontent.com` URLs on `main`, as the icon does, so nuget.org shows them; they render only once merged.
