# Source191.Vantage.Tui

The Terminal.Gui layer: screens, dialogs and table sources. It only renders and runs effects; what a key does is decided by Core's reducers. Depends on Core only.

- **No `InvariantGlobalization`:** only an executable's `runtimeconfig.json` carries the setting at run time, so on a library it would do nothing but switch off the culture analysers (CA1304, CA1305, CA1309, CA1310, CA1311), which then check this project's code. The tool project, `src\Source191.Vantage`, keeps it, so the shipped tool still runs invariant.
