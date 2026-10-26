# Proposal

## Why

Since `system-commandline`, `vantage here` takes the first token that is not a known option as the session's name, whatever it looks like. A mistyped option on its own, such as `vantage here --promt`, therefore starts a session called `--promt` instead of saying the option is wrong. Nobody means to name a session after an option, so a name that starts with `-` should be refused unless the user asks for it by putting `--` first.

## What Changes

- `vantage here` refuses a name that starts with `-` when it did not come after `--`: it writes a sentence naming the token and how to use it as a name, then the help for `vantage here`, starts nothing and exits 1, as a parse error does.
- `vantage here -- -experiment` still names a session `-experiment`; a prompt that starts with `-` (`-p "-fix the tests"`) is unaffected.
- The README's paragraph about the trap becomes a description of the refusal.

Out of scope: the same rule in the new-session wizard (a name typed there is never parsed as an option); suggestions of close matches for the refused token; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `command-line`: a new requirement that a name which looks like an option is refused unless it follows `--`.

## Impact

- `Core`: the `here` action in `Application\CommandLine` checks the parse result before calling the action; one sentence in `Application\Wording`.
- Tests: `CommandLineShould` (refused, accepted after `--`, a dash-led prompt still accepted) and a `WordingShould` fact.
- Docs: README's `vantage here` paragraph. No version change of its own: it ships in the same release as system-commandline (0.14.0).
