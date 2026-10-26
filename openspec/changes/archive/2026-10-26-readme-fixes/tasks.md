# Tasks

## 1. README

- [x] 1.1 Remove `-d "Flaky orders suite"` from the `vantage here` example; verify `Select-String -Path README.md -Pattern '-d "'` finds nothing
- [x] 1.2 Change "The The `e` key" to "The `e` key" in Settings; verify `Select-String -Path README.md -Pattern 'The The'` finds nothing
