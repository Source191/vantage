# Tasks

## 1. Domain

- [x] 1.1 TabColourSelectionShould: PreferAColourWhenItIsFree and FallBackToTheFirstFreeColourWhenThePreferredOneIsInUse; `Choose(inUse, preferred)`; verify they pass

## 2. Adapter text

- [x] 2.1 TabEscapesShould: title, clear-title, background for a palette colour (rgb:16/c6/0c on entry 264) and the DECAC reset; verify they pass

## 3. Own tab claim

- [x] 3.1 OwnTabShould: ColourItselfGreenWhenGreenIsFree (registration "vantage" claimed by the given process with its start time), TakeTheFirstFreeColourWhenGreenIsUsed, ClaimNothingWhenItsProcessIsUnknown; verify they pass
- [x] 3.2 TabOpenerShould: a live "vantage" registration's colour is skipped for the next session tab; verify it passes

## 4. Wiring and text

- [x] 4.1 Program.cs writes the title and colour before the first screen and the resets after the loop; README (the Vantage tab paragraph, the tabColor limitation, manual check); verify `dotnet build`, hand-off check, and by hand that the tab turns green and is named Vantage
