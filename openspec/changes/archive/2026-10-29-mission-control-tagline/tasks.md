# Tasks

## 1. The tool

- [x] 1.1 Red: `CommandLineShould.DescribeVantageAsMissionControl` runs `--help` and expects the new tagline; green: the root description in `CommandLine.Build` becomes the tagline; verify with the Core test run
- [x] 1.2 Set the csproj `Description` to the tagline and verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass

## 2. Header and README

- [x] 2.1 Let `tools\New-Header.ps1` draw any number of tagline lines, set the tagline over three lines, re-run it and verify the size checks pass, each image reads well by eye, and the cover still clears the Buy Me a Coffee card zone
- [x] 2.2 Replace the README's opening line and paragraph with the introductory paragraph and update the banner's alt text; verify no copy of the old tagline is left outside `openspec\config.yaml`, `CLAUDE.md` and the archive

## Workflow follow-up

- Set the repository's About description with `gh repo edit --description` once the pull request is pushed.
- Archive the change in the same pull request.
