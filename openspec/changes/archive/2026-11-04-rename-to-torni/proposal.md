# Proposal

## Why

Vantage has not been submitted to winget yet, so this is the last point at which the name can change without moving anyone's installation. The owner has chosen Torni (Finnish for tower, a lookout over every session): five letters, quick to type, and free on GitHub and winget. The mark was a V, so it is redrawn as a T. The tagline changes at the same time, because "mission control" is another tool's tagline and the old one names Claude Code, which a later version may not be limited to. And the docs repeat the name in many sentences where the repository already says what they are about, so those sentences lose it.

## What Changes

- **BREAKING:** the program is `torni` (`torni.exe`), with `torni here` and `torni setup-terminal`; the winget identifier is `Source191.Torni`, and the release zip is `torni_<version>_win-x64.zip`.
- **BREAKING:** settings, state, the crash log and local installs live in `%LOCALAPPDATA%\Torni`, the Windows Terminal fragment in `Fragments\Torni\torni.json`, and the profiles are named "Torni" and "Torni session". Nothing is migrated from `%LOCALAPPDATA%\Vantage`: no one has installed from winget, and the owner moves their own folder by hand.
- **BREAKING:** the environment variables are `TORNI_LOG`, `TORNI_NO_UPDATE_CHECK` and `TORNI_CLAUDE_CONTRACT`, and Torni's own tab is recorded in the state file as `torni`.
- Every user-facing word, tab title, dialog and help line says Torni; the GitHub links (issues, release pages, the changelog at a tag) point at `Source191/torni`, and the update check reads `manifests/s/Source191/Torni` in winget-pkgs.
- The tagline becomes "Every agent session in one tab, still there after a reboot." in the project description, `--help`, the README, the header images and the repository description.
- The mark becomes a T: two worktree branches meeting in a bar over a stem to one node, on the same tile and in the same colours, so the icon, the console's header icon, the header images and the README screenshot are redrawn. The icon files are named for the mark, not the product (`assets\icon\logo-<size>.png`, `logo.ico`, `logo-48.rgba`), and the zip and the fragment folder carry `logo.ico`, so a later rename leaves them alone. For the same reason the mark script is `tools\LogoMark.ps1`, the console's icon view `LogoIcon`, and the types and folder that only meant "this program" say what they are: `IUserConfig` and `UserConfig` (config.json), `OwnProcess`, `SelfUpdate` (the update key's effect) and the README scene's `C:\ReadmeDemo`.
- The projects, namespaces, solution, folders, files, tools, workflows, docs, `.claude\rules`, `CLAUDE.md`, the unreleased changelog entries and the unarchived OpenSpec changes follow.
- The human docs (README, user guide, performance monitoring, CONTRIBUTING, MAINTAINING, SECURITY, the project READMEs) and the agent docs (`CLAUDE.md`, `.claude\rules`) drop the name where the sentence reads as well without it; commands, paths, profile and variable names, titles and each document's first introduction keep it, as does any sentence that must tell Torni apart from Claude Code, Windows Terminal or another program.
- `Version` rises to 0.21.0-preview.1: a minor step, which may break while the tool is 0.y.z.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

Every requirement that names the program, its folders, its variables or its profiles is reworded with the new name; the rules themselves do not change. The requirements whose titles name it are renamed.

- `command-line`, `config-tab`, `console-layout`, `detail-pane`, `diagnostics`, `external-tools`, `folder-actions`, `needs-input-notification`, `problem-reports`, `pull-requests`, `release-notes`, `session-attach`, `session-launch`, `session-listing`, `settings`, `tool-updates`, `worktree-management`: the name, command, folder, variable and profile names in their requirements and scenarios.

## Impact

- **Code:** every project and namespace (`Source191.Torni.*`), `Wording`, `CommandLine`, `SharedParts` (the state folder), `TerminalFragment` and `TerminalProfiles`, `TabOwnerJsonConverter`, `UpdateCheckOptOut`, `WingetPackageVersions`, `ReleasePage`, `ProblemReportAddress`, the tool's `AssemblyName` and icon.
- **Workflows and scripts:** `pipeline.yml`, `publish.yml`, `claude-contract.yml`, and `ToolPaths.ps1`, `Assert-VersionBumped.ps1` and the release step, which read the base's Version from the old project path when the new one is not there yet.
- **Docs and assets:** README, user guide, CONTRIBUTING, MAINTAINING, SECURITY, CLAUDE.md, `.claude\rules`, the unreleased changelog section, `TorniMark.ps1` and every image drawn from it (icon sizes, `.ico`, the console's icon pixels, header images), and the README screenshot and animation.
- **Outside the repository, by the owner:** renaming the GitHub repository, its description and social preview, the Buy Me a Coffee cover, and their own `%LOCALAPPDATA%\Vantage` folder and Windows Terminal fragment; listed in the pull request.

## Out of scope

- Multiple Claude accounts, which is a later change.
- Integrating coding agents other than Claude Code; the tagline only leaves room for it.
- Migrating `%LOCALAPPDATA%\Vantage` or the old fragment in code.
- The archived changes (`openspec\changes\archive`) and the released changelog sections, which stay as history under the old name.
- The nuget.org package `Source191.Vantage`, which keeps its name until it is retired as `install-with-winget` describes.
- Trimming the name from the specs, code comments, the changelog and the text the program shows, where requirements need a subject and the screen is read outside the repository.
