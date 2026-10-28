# Design

## Context

`FolderDeletion.DeleteTree` (behind `IFolders.DeleteTree`) is called only by `RemoveWorktreeHandler`, for orphan, broken and empty rows. It returns quietly for a missing folder. When it throws, `TableScreen` catches the exception and shows `Footer.Failed` with its message. Its tests delete real folders under a throwaway temporary root.

## Goals / Non-Goals

**Goals:**
- The refusal happens before any check or change on disc, so a missing root is refused too, not quietly ignored.
- No test can ever delete a real root, even with the guard missing or wrong.

**Non-Goals:**
- A list of protected folders beyond roots, such as the user profile or `C:\Windows`.

## Decisions

- **The rule:** refuse when `Path.IsPathFullyQualified` is false (`Northwind.Orders-fix`, `C:`, `\Users`). Otherwise refuse when the full path, with trailing separators trimmed, equals its own `Path.GetPathRoot`, likewise trimmed (`C:\`, `\\server\share\`). It lives as a private guard at the top of `DeleteTree`. Only the adapter deletes, so a Domain rule would have one caller.
  - *Rejected: a `FolderPath` invariant.* Roots are valid folders elsewhere, for example as repository roots in settings.
- **The exception:** an `IOException` whose message names the path and says Vantage will not delete a drive root, a share root or a relative path. The message reaches the user through `Footer.Failed`, which CLAUDE.md allows for reporting what failed. That rules out the throw helpers, whose messages append "(Parameter ...)", and rules out `ArgumentException`.
- **Tests aim only at targets that do not exist:**
  - **Drive root:** an unused drive letter, chosen by the test as the first of Z down to D that `DriveInfo.GetDrives()` does not list.
  - **Share root:** a server name that cannot resolve (`\\vantage-no-such-host\share\`).
  - **Relative path:** a GUID-named folder.
  - **Why:** without the guard each of these returns quietly (the folder does not exist), so the test fails on the missing exception and nothing is ever at risk. A test is never pointed at `C:\`, `\\localhost\C$\` or the working folder.

## Risks / Trade-offs

- [A guard that returns quietly instead of throwing would hide a bug] → it throws, so the user sees the failure and the bug surfaces.
- [`DriveInfo.GetDrives()` misses a mapped drive that is offline] → such a letter's root does not exist either, so the test stays harmless.
