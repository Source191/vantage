## MODIFIED Requirements

### Requirement: Failures are recorded with a summary
When an effect, a view's load, a dialog flow, the update check or a background task fails, Vantage SHALL record the failure with an exception summary: the type of the exception and of each inner exception, and the stack frames as method names with the source file's name and line where known, without the exception's message, any folder in a file path or any value. The screen SHALL show the failure as it does today.

#### Scenario: A load fails
- **WHEN** the sessions view's load throws an `InvalidOperationException` whose message names a folder
- **THEN** the status line shows the message as before, and the log records a load failure of the sessions view with `InvalidOperationException` and its frames, without the message

#### Scenario: Stack frames of a local build
- **WHEN** a Vantage built in C:\Users\me\source\repos\vantage fails inside `GitCli.RunOrThrowAsync`
- **THEN** the frame reads with the method name and `GitCli.cs` and its line, and "C:\Users\me" does not appear
