# Design

## Context

See proposal.md. In System.CommandLine 2.0.12 an unknown option-like token fills a free argument slot, so `here --promt` binds `--promt` to `name`. A validator on the `name` argument sees only that argument's tokens: a probe of `here -- -experiment` showed the argument, its command and the root all without the `--` token, which appears only in `ParseResult.Tokens`. `Token.Position` is internal.

## Goals / Non-Goals

**Goals:** refuse a dash-led name that did not follow `--`, in the shape of a library parse error (sentence, help, exit 1).

**Non-Goals:** changing how other commands bind arguments (`setup-terminal` has none); suggestions for the refused token.

## Decisions

- **Check in the `here` action, against `ParseResult.Tokens`.** The action finds the name's token in the parse result's token list by reference and refuses it when it starts with `-` and no `--` token comes before it. Alternatives: a validator on `name` (cannot see `--`); refusing every dash-led name in the validator or in `SessionName.Validate` (simpler, but removes the `--` escape the README documents and changes the wizard).
- **Refuse like the library does.** The action writes `Wording.NameLooksLikeAnOption(token)` to the invocation's error writer, then runs `HelpAction` for the parse result so the help for `vantage here` follows, and returns 1. The sentence is Vantage's, so it lives in `Wording`.

## Risks / Trade-offs

- [A library update starts reporting unknown options itself] → the check then never fires and `CommandLineShould` still passes for the refusal through the library's own error, with exit code 1.
