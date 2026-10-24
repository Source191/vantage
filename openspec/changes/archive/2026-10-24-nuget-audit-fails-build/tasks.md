# Tasks

## 1. Audit fails the build

- [x] 1.1 Remove the `WarningsNotAsErrors` exemption for `NU1900` to `NU1904` from `Directory.Build.props` and reword the comment; verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass
- [x] 1.2 Verify the rule bites: add a reference to a package version with a known advisory to a scratch project under the repository root, run `dotnet restore` and confirm it fails with an `NU190x` error, then delete the scratch project

## 2. Archive

- [x] 2.1 Archive the change and run `openspec validate`
