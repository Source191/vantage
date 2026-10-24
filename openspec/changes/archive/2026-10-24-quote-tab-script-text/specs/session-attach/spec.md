# Spec Delta

## ADDED Requirements

### Requirement: A tab runs its values as literal text
Every value Vantage places in the script of a tab it opens (an executable path, a session's job id, a registration id, a folder or repository name) SHALL reach the program as the same text, whatever characters it holds. No value, including one containing a straight or curly single quote, SHALL end the literal it sits in or be run as a command.

#### Scenario: Path with a curly apostrophe
- **WHEN** the Claude executable lives under a profile folder whose name contains a curly apostrophe, such as `C:\Users\O’Neill`
- **THEN** the session's tab runs that executable from that exact path and runs nothing else from the path's text

#### Scenario: Value that looks like a command
- **WHEN** a value in a tab's script holds a quote followed by PowerShell, such as `x'; Remove-Item C:\temp; '`
- **THEN** the tab prints or passes that text unchanged and does not run the command
