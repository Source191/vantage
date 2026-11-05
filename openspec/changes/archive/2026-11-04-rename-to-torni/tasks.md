## 1. Rename

- [x] 1.1 Move every tracked path that names the program, outside `openspec\changes\archive` and `openspec\specs`, with `git mv` (projects, solution, icon files, `TorniMark.ps1`). Verify `git ls-files` lists no other path with the old name.
- [x] 1.2 Rewrite the text of every tracked text file outside the archive, `openspec\specs` and this change (`Vantage`, `vantage` and `VANTAGE` to `Torni`, `torni` and `TORNI`), leaving the released sections of `CHANGELOG.md` as they are. Verify with `git grep -i vantage` that what remains is only the exceptions in design.md.
- [x] 1.3 Restore the exceptions: the nuget.org package `Source191.Vantage` in `install-with-winget`. Verify by reading each remaining match.
- [x] 1.4 `ToolPaths.ps1`'s `Read-ToolVersion` and `pipeline.yml`'s release step read the old project path when the new one is not at the commit. Verify with `Assert-VersionBumped.ps1 -Base origin/main -Head HEAD` passing from 0.20.0-preview.32 to 0.21.0-preview.1.
- [x] 1.5 `WordingShould.ShowWhatTheProfilesWillBe` and `WordingOfKeysAndColumnsShould`'s help facts fail on the padding the old name set; re-pad the profiles column in `Wording.TerminalSetupPlan` and the help facts' expected text. Verify the Core suite passes.
- [x] 1.6 Raise `Version` to 0.21.0-preview.1 and add the rename to the Unreleased section of `CHANGELOG.md` under Changed, ending with the pull request number. Verify with `Assert-ChangelogUpdated.ps1`.

## 2. Tagline

- [x] 2.1 `CommandLineShould`'s help fact expects "Every agent session in one tab, still there after a reboot."; red, then the root command's description in `CommandLine`; green.
- [x] 2.2 The tool's `Description`, `tools\New-Header.ps1`, the README's banner text and pitch line say the new tagline and no longer say mission control. Verify with `git grep -i "mission control"` outside the archive.

## 3. Mark and images

- [x] 3.1 Redraw the mark as a T in `TorniMark.ps1` and describe it so in its synopsis and the README's banner text. Verify by looking at the 512 px icon.
- [x] 3.2 Remake the icon sizes, `.ico` and console icon pixels with `New-Icon.ps1` and `New-IconPixels.ps1`, the header images with `New-Header.ps1`, and the README screenshot and animation with `New-ReadmeScreenshot.ps1`. Verify by looking at each image.
- [x] 3.3 `TerminalFragmentShould` expects the fragment's icon copy as `logo.ico`; red, then name the icon files `logo-*` and `logo.ico` and every reference (the tool and Tui projects, `TerminalFragment`, `SharedParts`, `SceneHost`, `Compress-WingetBuild.ps1`, the icon scripts, docs and rules); green.
- [x] 3.4 Rename `TorniMark.ps1` to `LogoMark.ps1` (`Add-LogoMark`), `TorniIcon` to `LogoIcon`, `ITorniConfig` and `TorniConfig` to `IUserConfig` and `UserConfig`, `TorniProcess` to `OwnProcess`, `UpdateTorni` to `SelfUpdate` and `C:\TorniDemo` to `C:\ReadmeDemo`, a refactor with no behaviour change. Verify the build and the whole suite pass unchanged.

## 4. Docs

- [x] 4.1 Take the name out of the README, user guide, performance monitoring, CONTRIBUTING, MAINTAINING, SECURITY and project READMEs wherever design.md's trimming rule allows. Verify by reading each changed sentence in the diff.
- [x] 4.2 The same for `CLAUDE.md` and `.claude\rules`. Verify by reading each changed sentence in the diff.

## 5. Check and archive

- [x] 5.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes` pass.
- [x] 5.2 Archive the change, then reword the Purpose lines and the scenario titles in `openspec\specs` that still name the program. Verify with `openspec validate --all` and `git grep -i vantage -- openspec/specs` finding nothing.
