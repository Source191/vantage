# Spec Delta

## ADDED Requirements

### Requirement: A hand-edited settings file is read leniently but not twice
A `config.json` that names the same property twice SHALL read as a corrupt file does, every setting its default. A property Torni does not know SHALL be ignored and every other setting kept, so a file written by a newer or older version still reads. A property's name SHALL be matched whatever its letter case.

#### Scenario: A setting named twice
- **WHEN** `config.json` holds `repositoryRoots` of `D:\work` and then `repositoryRoots` again of `E:\other`
- **THEN** Torni starts with every default, as it does for a corrupt file, and does not exit

#### Scenario: A property Torni does not know
- **WHEN** `config.json` holds `repositoryRoots` of `D:\work` and a `theme` property no version of Torni writes
- **THEN** Torni scans `D:\work` and the other settings read as their defaults

#### Scenario: A file from a newer version
- **WHEN** a newer Torni has written `config.json` with every setting this version knows and one it does not
- **THEN** this version reads every setting it knows, and a save from the Config tab keeps them

#### Scenario: A name in another letter case
- **WHEN** `config.json` holds `RepositoryRoots` of `D:\work`
- **THEN** Torni scans `D:\work`

#### Scenario: A null where a setting belongs
- **WHEN** `config.json` holds `repositoryRoots` of `D:\work` and `worktreeRoot` of `null`
- **THEN** Torni scans `D:\work` and puts new worktrees under `D:\worktrees`, as it does when `worktreeRoot` is missing
