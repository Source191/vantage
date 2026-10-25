# Tasks

## 1. The rule

- [x] 1.1 `UpdateCheckOptOutShould.AppliesToAnyNonBlankValue`, `NotApplyWhenUnsetEmptyOrBlank`: implement `Domain\UpdateCheckOptOut`
- [x] 1.2 Wire it in `Program.cs`: `enabled` is the setting and not the opt-out (`UpdateCheckShould` already covers a disabled check)

## 2. The harness

- [x] 2.1 `DriverOptionsShould.SetTheUpdateCheckOptOutForTheChildWhateverItInherited`: set it in `ChildEnvironment`

## 3. Documentation and release

- [x] 3.1 README beside `checkForUpdates`, and the harness README
- [x] 3.2 Raise `Version` to 0.11.1
- [ ] 3.3 Run the hand-off check once and confirm it wrote no `update-check.json`
