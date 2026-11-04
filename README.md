# Torni

![Torni: every agent session in one tab, still there after a reboot. Beside it, the icon: two branches meeting in the bar of a T, over a stem to one node](https://raw.githubusercontent.com/Source191/torni/main/assets/header/readme-banner.png)

<a href="https://buymeacoffee.com/simonvane"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-orange.png" alt="Buy me a coffee" height="40"></a> &nbsp; [![Built by Simon Vane](https://img.shields.io/badge/Built_by-Simon_Vane-0A66C2?style=for-the-badge)](https://www.linkedin.com/in/simonvane/)

Five Claude Code sessions. A dozen terminal tabs. One of them finished ten minutes ago, another has been waiting for your permission since lunch, and you cannot remember which tab is which.

**Torni keeps every Claude Code session on Windows in one place:** one tab that lists every session on the machine, rings when one needs you, and opens any of them in its own coloured tab.

![Torni's Sessions tab in Windows Terminal, with five coloured session tabs beside it. Eight sessions are listed in Northwind repositories and Torni's own: one needs input, two are working, two are idle, one completed, one failed and one stopped, with GitHub and Azure DevOps pull requests beside them. The detail pane shows that the selected session is waiting for permission to run dotnet test.](https://raw.githubusercontent.com/Source191/torni/main/assets/readme/sessions.png)

- **See every session at once:** its state, repository and pull request, on GitHub or Azure DevOps, whether Torni started it or not.
- **Start each in a fresh worktree:** sessions never trip over each other's changes.
- **One coloured tab per session:** Enter opens it, or switches to it when it is open.
- **Hear when one needs you:** the bell rings and its row turns amber.
- **Close the terminal, keep the sessions:** they run under Claude Code's own supervisor, so they carry on with no terminal showing them, are still listed after a reboot and resume with one key.

## Why I built it

I wanted a tool that worked the way I work. I tried [Orca](https://github.com/stablyai/orca) and [Kepler](https://gitkraken.com/kepler) first, and there is a lot I like about both, but I live in the terminal, I want to stay there, and both do a great deal I was not interested in. By all means try the others, but give Torni a go too 😊

Built by [Simon Vane](https://www.linkedin.com/in/simonvane/).

## Support Torni

Torni is built in the gaps between rate limits. If it saves you ten minutes a day, a coffee now and then keeps it moving: every one goes towards the Claude plan it is built with, and if you use it every day, a small monthly membership is there too.

<a href="https://buymeacoffee.com/simonvane"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-orange.png" alt="Buy me a coffee" height="40"></a>

<img src="https://raw.githubusercontent.com/Source191/torni/main/assets/support/buy-me-a-coffee-qr.png" alt="QR code for buymeacoffee.com/simonvane" width="120">

## Install

```powershell
winget install Source191.Torni
```

That brings Windows Terminal, PowerShell 7 and git if any of them is missing, and Torni carries its own .NET. The one thing it cannot bring is Claude Code 2.1.309 or later, which comes from its own installer:

```powershell
irm https://claude.ai/install.ps1 | iex
[Environment]::SetEnvironmentVariable('Path', [Environment]::GetEnvironmentVariable('Path', 'User') + ";$HOME\.local\bin", 'User')   # puts claude on the PATH
```

Open a new terminal, run `claude` once to sign in, then `torni`. Torni tells you when a newer release is out, and `U` upgrades it. The [user guide](https://github.com/Source191/torni/blob/main/docs/user-guide.md#installing) covers installing without winget, previews and uninstalling.

## Getting started

1. **Create a session:** run `torni` in an ordinary Windows Terminal tab and press `n`. Choose a repository (Torni looks under `~\source\repos`; change that on the Config tab), give the session a name and, if you like, an opening prompt, then press F5. It gets its own worktree and opens in a coloured tab named after it.

   ![Starting a session: n opens the new-session wizard, Northwind.Inventory is chosen, add-stock-alerts is typed as the name with an opening prompt, F5 opens the session in a purple tab, and back in Torni the new session is listed as working.](https://raw.githubusercontent.com/Source191/torni/main/assets/readme/new-session.gif)

2. **See your sessions:** the Sessions tab lists every session with what it is doing. Amber means it needs you, and the Torni tab rings. Enter switches to a session's tab.
3. **Run several:** press `n` again for each piece of work; each gets its own worktree and tab, all in the one list. Already in a folder? `torni here` starts a session there.
4. **Close the terminal; your sessions stay:** closing tabs, Torni or Windows Terminal stops nothing. Start Torni again and every session is still listed, and Enter takes you back. After a reboot they show as stopped, and `R` resumes them all.
5. **Use the Torni profiles:** run `torni setup-terminal` and restart Windows Terminal. It adds a Torni profile (open Torni from the new-tab menu, or make it the startup profile) and a Torni session profile for session tabs, each with its own bell, and turns on Claude Code's terminal bell, so you hear both when a session needs you. See [notifications and profiles](https://github.com/Source191/torni/blob/main/docs/user-guide.md#notifications-and-profiles).

`?` on any tab lists every key, and the key bar shows the ones for the selected row.

Got your first session running? Buy me a sliver of a Claude Max plan:

<a href="https://buymeacoffee.com/simonvane"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-orange.png" alt="Buy me a coffee" height="40"></a>

## Learn more

- **User guide:** [docs/user-guide.md](https://github.com/Source191/torni/blob/main/docs/user-guide.md), every key, column, setting and command.
- **In the tool:** `?` on any tab, and a tooltip on every column header and key; `torni --help` for the commands.
- **What changed:** [CHANGELOG.md](https://github.com/Source191/torni/blob/main/CHANGELOG.md).
- **Privacy:** Torni sends nothing but a daily, anonymous check of the winget community repository on GitHub for a newer release, and of that release's changelog (turn it off on the Config tab), and the problem reports you choose to submit. It stores no credential and records nothing personal.
- **Security:** to report a vulnerability, see [SECURITY.md](https://github.com/Source191/torni/blob/main/SECURITY.md).
- **Contributing:** [CONTRIBUTING.md](https://github.com/Source191/torni/blob/main/CONTRIBUTING.md).
