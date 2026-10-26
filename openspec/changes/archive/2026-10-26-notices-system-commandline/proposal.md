# Proposal

## Why

`system-commandline` (0.14.0) made System.CommandLine 2.0.12 a shipped dependency, but `THIRD-PARTY-NOTICES.txt`, which travels in the tool package so every bundled licence's notice goes with it, does not name it. Three Microsoft.Extensions entries also still give versions that Dependabot has since raised, and Terminal.Gui's satellite resource assemblies are not mentioned.

## What Changes

- `THIRD-PARTY-NOTICES.txt` gains System.CommandLine 2.0.12 (MIT, .NET Foundation and Contributors), with its satellite resource assemblies.
- Microsoft.Extensions.DependencyInjection.Abstractions, FileSystemGlobbing and Logging.Abstractions read 10.0.14, the versions now packed.
- Terminal.Gui's entry names its satellite resource assemblies too.
- No behaviour change, no spec change and no version bump: the notices file sits at the repository root, outside the paths that require one; the next release carries it.
