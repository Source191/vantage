# Tasks

## 1. The support line in vantage --help

- [x] 1.1 Red: `WordingShould.SayWhereToSupportVantage` expects `Wording.SupportLine` to be `Buy me a sliver of a Claude Max plan: https://buymeacoffee.com/simonvane`; green: add `SupportUrl` and `SupportLine` to `Wording`; verify with the Core test run
- [x] 1.2 Red: `CommandLineShould.SayWhereToSupportVantageInTheHelp` runs `--help` and expects the support line in standard output; green: append `Wording.SupportLine` on a second line of the root description in `CommandLine.Build`; verify with the Core test run and that the existing help facts still pass
- [x] 1.3 Raise `Version` to `0.17.0-preview.3` and verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass, and that `vantage --help` from the Debug build shows the line

## 2. Header artwork

- [x] 2.1 Move the mark's drawing out of `tools\New-Icon.ps1` into `tools\VantageMark.ps1` (`Add-VantageMark`), dot-sourced by `New-Icon.ps1`; verify by re-running `New-Icon.ps1` and seeing no change under `assets\icon\` in `git status`
- [ ] 2.2 Write `tools\New-Header.ps1` (comment-based help in `New-Icon.ps1`'s style) drawing the cover (3200 x 800), README banner (1600 x 400) and social preview (1280 x 640) to `assets\header\`, and the 320 px QR code to `assets\support\buy-me-a-coffee-qr.png` from a `-QrCode` path; verify the script's own size checks pass and inspect each PNG by eye
- [ ] 2.3 Check the cover against the Buy Me a Coffee layout: overlay the card zone (middle 1000 px, bottom fifth, at the page's 2000 x 390 display) on a scaled copy in `$CLAUDE_JOB_DIR\tmp` and verify no text or mark falls inside it

## 3. README and GitHub

- [ ] 3.1 Replace the README's icon line with the banner, add the shields badge linking to the page under it, add the "Support Vantage" section before "Roadmap and known limits" with the rate-limits wording, the link and the QR image, and add it to Contents; verify every image and link is an absolute URL and the Contents anchor matches the heading
- [ ] 3.2 Add `.github\FUNDING.yml` with `buy_me_a_coffee: simonvane`; verify the key against GitHub's documented FUNDING.yml keys
- [ ] 3.3 Run `openspec validate support-links` and verify it passes

## Workflow follow-up

- Archive the change in the same pull request, so `openspec\specs\command-line` carries the support line.
- Commit per step on `v-buy`, push and open the pull request; after merge, upload `assets\header\buy-me-a-coffee-cover.png` as the Buy Me a Coffee cover and `assets\header\social-preview.png` under the repository's Settings > General > Social preview.
