# Tasks

## 1. Reducers

- [x] 1.1 SessionsViewShould.KnowWhetherTheFirstListingHasArrived and WorktreesViewShould.KnowWhetherTheFirstScanHasArrived: `Loaded` false on Empty and after LoadFailed, true after the first rows; verify both pass

## 2. Screens

- [x] 2.1 SessionsScreen: a footer `SpinnerView` shown with AutoSpin by `RunInBackground` and hidden in its continuation; the wizard start moves onto `RunInBackground`; a centred `SpinnerView` and "Listing sessions" label shown while not loaded and no banner; verify `dotnet build -c Debug`
- [x] 2.2 WorktreesScreen: the same two spinners with "Scanning worktrees"; verify `dotnet build -c Debug`
- [x] 2.3 README: one sentence under the sessions view describing the spinners; verify `dotnet format --verify-no-changes`

## 3. Integration check

- [x] 3.1 Hand-off check exits 0, and a `drive` capture of a cold start shows a braille spinner glyph before the first rows (count with `count drive-raw.txt "Listing sessions"`)

## Workflow follow-up

- Archive the change (`openspec archive busy-spinners -y`).
