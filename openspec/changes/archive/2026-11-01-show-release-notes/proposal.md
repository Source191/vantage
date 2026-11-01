# Proposal

## Why

A newer release is announced only by a few plain words at the end of the settings row (`0.19.0 available, U to update`), which is easy to miss, and nothing tells the user what the release changes, so there is no reason to press `U`. The notes that do exist are GitHub's generated ones: pull request titles written for contributors, and for a stable release cut after a run of previews they list only the pull requests since the last preview.

## What Changes

- The update notice in the settings row is drawn in amber, so it stands out from the white values beside it; its words are unchanged.
- The first time the check finds a release newer than the one it had recorded, the status line says so once, with the version and `U` to see what changed.
- `U` opens a "What's new" dialogue in place of the yes or no question: the changes in every stable release after the running one up to the newest, newest first, each line with its pull request number, and three buttons: Update now, Release page (opens the newest release's GitHub page in the browser) and Later, with Later focused so a stray Enter does nothing.
- The check reads the notes from the newest release's package on nuget.org (its nuspec `releaseNotes`), the host it already asks, with the same anonymous 3 second request, only when it finds a newer release; they are recorded with the newest version so `U` never waits. When they cannot be read, the dialogue says so and still offers Update now and Release page.
- After an update, the first start of the new version shows "What's new in 0.21.0" once, from the notes shipped inside the package itself, covering every stable release since the version that last ran; a first ever start, a development build and a start of the same version show nothing.
- A curated, user-facing `CHANGELOG.md` at the repository root (Keep a Changelog sections Added, Changed, Fixed, Removed, Security, plus Internal, which Vantage never shows) becomes the one source of release notes: every pull request that changes the packaged tool adds a line under Unreleased, enforced by a new check beside `Assert-VersionBumped.ps1`; a pull request that raises `Version` to a stable version turns Unreleased into that version's section.
- Packing puts the changelog in the package twice, as it is: as `PackageReleaseNotes` (what the check reads) and as a file inside the tool (what the first start of a new version reads); Vantage drops Unreleased and Internal when it reads either.
- A stable GitHub Release's notes are its changelog section, with generated notes starting at the previous stable tag, so they cover all its previews.
- Text from the notes is treated as text from outside Vantage: parsed with `TryParse`, control characters removed before it is drawn, and never used to build a URL; the Release page URL is built from the version alone.
- README (Updates, What the check sends, state files) and `CONTRIBUTING.md` (the changelog line in a pull request, and in a stable release) say what changed.

Out of scope: a dialogue that opens on start when a release is found (left as an open question in the design), changing the Windows Terminal tab title while an update is available (also an open question), notes for prerelease versions, translating the notes, clickable links inside the dialogue, rewriting the notes of releases already published, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

- `release-notes`: the changelog as the source of user-facing release notes, how it reaches the package and the GitHub Release, and how Vantage reads and shows it (the What's new dialogue before updating and once after).

### Modified Capabilities

- `tool-updates`: the check also records the newest release's notes; the header's notice is amber; a newly found release is announced once in the status line; `U` shows the What's new dialogue instead of a yes or no question.
- `console-layout`: the settings row's update notice is drawn in amber rather than white.

## Impact

- `Domain`: a `ReleaseNotes` value (sections by version, each with its entries; `TryParse` over changelog text) and the rule choosing the sections between two versions.
- `Ports`: `IPackageVersions` gains a notes request (or a sibling port), `UpdateCheckRecord.Checked` carries the notes, a new port for the packaged notes and one for the last version run.
- `Application`: `UpdateCheck` (notes and the announcement), `UpdateVantage.Decide` (the dialogue's content), a `ShowWhatsNew` effect at start, `StatusMessage.NewerReleaseFound`, `Wording` and its facts.
- `Adapters`: `NuGetPackageVersions` reads `.../source191.vantage/<version>/source191.vantage.nuspec`; `JsonFileStore` writes the notes into `update-check.json` and the last version run into a new `last-run.json`; a reader for the packaged changelog.
- `Tui`: a `WhatsNewDialog` replacing `Confirm.Ask` in the update flow, and the amber notice; `SmokeScript` gains its step.
- Repository: `CHANGELOG.md`, `src\Source191.Vantage\Source191.Vantage.csproj` (`PackageReleaseNotes` and the packed file), `.github\scripts\Assert-ChangelogUpdated.ps1`, `check.yml`, `release.yml`; README and `CONTRIBUTING.md`; `Version` raised (minor) when implemented.
