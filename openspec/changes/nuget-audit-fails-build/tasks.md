# Tasks

## 1. Audit fails the build

- [ ] 1.1 Remove `NU1901;NU1902;NU1903;NU1904` from `WarningsNotAsErrors` in `Directory.Build.props`, keeping `NU1900`, and reword the comment; verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass
- [ ] 1.2 Verify the rule bites: add a reference to a package version with a known advisory to a scratch project under the repository root, run `dotnet restore` and confirm it fails with an `NU190x` error, then delete the scratch project

## 2. Archive

- [ ] 2.1 Archive the change and run `openspec validate`
