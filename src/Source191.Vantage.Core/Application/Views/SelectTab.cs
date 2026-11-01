namespace Source191.Vantage.Application.Views;

/// <summary>Show this tab: 1, 2 and 3, a click on its title, and Esc back to Sessions.</summary>
public sealed record SelectTab(ConsoleTab Tab) : ViewEffect, ISwitchesTab;