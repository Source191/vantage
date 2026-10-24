# Proposal

## Why

`ClaudeAgentsListing.Row` is the last type in the solution that carries `[property: JsonPropertyName]` attributes, ten of them, one per field. They are redundant: every C# name already matches its JSON key case-insensitively and the parser's options set `PropertyNameCaseInsensitive = true`. The state files in `JsonFileStore` already read and write without attributes (converters and a type-info modifier on the options), so the listing parser is the odd one out.

## What Changes

- `ClaudeAgentsListing.Row` loses its ten `JsonPropertyName` attributes and becomes a plain positional record.
- `ClaudeAgentsListing.Options` is built from `JsonSerializerDefaults.Web` (camelCase naming, case-insensitive reads), the same defaults `JsonFileStore` uses, so the two serialisers in the solution are configured alike.
- `Version` rises from 0.4.5 to 0.4.6: the change touches `src\`, and every such pull request is a release.

Out of scope: how the listing is mapped to sessions, the state file formats, source-generated serialisation, multiple Claude accounts.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The parser accepts the same JSON and produces the same sessions; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `src\Source191.Vantage.Infrastructure\Claude\ClaudeAgentsListing.cs`: attributes removed, options changed, the `System.Text.Json.Serialization` using goes.
- `src\Source191.Vantage\Source191.Vantage.csproj`: version bump.
- Tests: `ClaudeAgentsListingShould` already parses a real `claude agents --json --all` capture with every field present, so it is the acceptance test and needs no new case. The one tolerance `Web` defaults add, reading a number from a quoted string, is harmless and not relied on.
