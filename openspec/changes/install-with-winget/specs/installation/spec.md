# Spec Delta

## Purpose

Says how Vantage reaches a machine: installed and upgraded with winget as a self-contained program, with the prerequisites winget can bring, previews tried from a zip, and a dotnet tool install moved across.

## ADDED Requirements

### Requirement: Stable releases install with winget
Each stable release SHALL be installable with `winget install Source191.Vantage` on Windows 10 or 11 with no .NET SDK or runtime on the machine. The installed `vantage` command SHALL run from any new terminal and print the release's version with `--version`.

#### Scenario: A machine with no .NET
- **WHEN** `winget install Source191.Vantage` completes on a machine with no .NET installed and the user opens a new terminal and runs vantage --version
- **THEN** standard output is the installed release's version and the exit code is 0

#### Scenario: Upgrading
- **WHEN** a newer stable release is in the winget community repository and the user runs `winget upgrade Source191.Vantage` while no Vantage runs
- **THEN** the installed version becomes the newer release and the user's `config.json` and state files are untouched

#### Scenario: Uninstalling
- **WHEN** the user runs `winget uninstall Source191.Vantage`
- **THEN** the `vantage` command is removed and the user's `config.json`, state files and Claude Code sessions are untouched

### Requirement: The package brings the prerequisites winget can
The winget package SHALL declare Windows Terminal, PowerShell 7 and git as package dependencies, so that installing it installs any of them that is missing. It SHALL NOT declare Claude Code, because the winget Claude Code package can be older than the version Vantage needs and does not update itself.

#### Scenario: Nothing installed yet
- **WHEN** a machine has none of Windows Terminal, PowerShell 7 or git and the user runs `winget install Source191.Vantage`
- **THEN** all three are installed before Vantage

#### Scenario: Claude Code is not installed for the user
- **WHEN** the user runs `winget install Source191.Vantage` on a machine with no Claude Code
- **THEN** Claude Code is not installed, and starting Vantage says Claude Code is missing as it does today

#### Scenario: The README names what is left
- **WHEN** a person reads the install section of the README
- **THEN** it gives `winget install Source191.Vantage` and the one remaining prerequisite, Claude Code 2.1.309 or later with its native installer, and names no other prerequisite

### Requirement: A release carries a self-contained build
Every release published by the release workflow, stable or prerelease, SHALL attach a zip of the self-contained single-file `win-x64` build to its GitHub Release, with build provenance attested for the repository's release workflow. The zip SHALL hold the executable and every file it reads from beside itself, including the changelog and the icon.

#### Scenario: A stable release
- **WHEN** a push to main raises `Version` to 0.21.0
- **THEN** the GitHub Release v0.21.0 carries `vantage_0.21.0_win-x64.zip` with a provenance attestation, and the zip holds `vantage.exe`, `CHANGELOG.md` and `vantage.ico`

#### Scenario: A prerelease
- **WHEN** a push to main raises `Version` to 0.21.0-preview.3
- **THEN** the GitHub prerelease v0.21.0-preview.3 carries the same kind of zip, and nothing is submitted to the winget community repository

### Requirement: Stable releases reach the winget community repository
After a stable GitHub Release is published, its manifest SHALL be submitted to the winget community repository as a pull request that names the release's zip and its SHA-256, so that the version becomes installable once that pull request is merged. A prerelease SHALL never be submitted.

#### Scenario: A stable release is published
- **WHEN** the release workflow publishes v0.21.0
- **THEN** a pull request to the winget community repository adds 0.21.0 of Source191.Vantage, whose installer URL is the release's zip and whose SHA-256 is the zip's

#### Scenario: A prerelease is published
- **WHEN** the release workflow publishes v0.21.0-preview.3
- **THEN** no pull request is made to the winget community repository

### Requirement: Previews are tried from the zip
A preview SHALL be installable from the zip of a pull request check's artefact or of a GitHub prerelease, extracted to a folder the user chooses, and `tools\install-local.ps1` SHALL do the extraction and report the path of the executable. A preview SHALL NOT replace or alter an installed winget package.

#### Scenario: A preview from a check run
- **WHEN** a developer runs `tools\install-local.ps1 -FromArtifact` on a downloaded check artefact
- **THEN** its executable is extracted beside the script's chosen folder, its full path is printed and `vantage --version` run from that path prints the preview's version

#### Scenario: A winget install is present
- **WHEN** a winget-installed Vantage exists and a preview is installed locally
- **THEN** the `vantage` command on the PATH still runs the winget-installed version

### Requirement: A dotnet tool install offers to move to winget
A Vantage installed as a dotnet tool SHALL say, in its header and on the update key, that Vantage is now installed with winget, and the update key SHALL after confirmation run `winget install Source191.Vantage`, then `dotnet tool uninstall -g Source191.Vantage` only when the install succeeded, then start the winget-installed Vantage. This applies only to the last release published to nuget.org.

#### Scenario: Moving across
- **WHEN** a dotnet tool Vantage runs, winget has Vantage 0.21.0, and the user presses U and confirms
- **THEN** a tab opens, Vantage quits, the tab waits for it, installs the winget package, uninstalls the dotnet tool and starts the new Vantage

#### Scenario: The winget install fails
- **WHEN** `winget install Source191.Vantage` fails in the tab
- **THEN** the dotnet tool is not uninstalled, the tab stays open showing winget's output and Vantage is not started again

#### Scenario: Declined
- **WHEN** the user presses U and chooses Later or presses Esc
- **THEN** nothing is installed or uninstalled and Vantage stays on the same view and selection
