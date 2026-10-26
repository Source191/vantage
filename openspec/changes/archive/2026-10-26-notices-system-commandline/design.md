# Design

## Context

Documentation only; see proposal.md.

## Decisions

Checked by packing the tool and comparing every DLL under `tools\` in the nupkg (other than Vantage's own) with the notices file, and the versions with `dotnet list src\Source191.Vantage package --include-transitive`. After the change every packed DLL is named or covered by its package's entry.
