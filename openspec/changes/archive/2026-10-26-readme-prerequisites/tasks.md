# Tasks

## 1. README

- [x] 1.1 Add the Prerequisites section before Install and its line in Contents; verify `Select-String -Path README.md -Pattern '^## Prerequisites','\(#prerequisites\)'` finds both
- [x] 1.2 Replace Install's prerequisite line with a link to the section; verify `Select-String -Path README.md -Pattern '^Prerequisites:'` finds nothing