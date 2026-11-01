namespace Source191.Vantage.Application.Views;

/// <summary>Edit one setting in a dialog of its own; the dialog settles on <see cref="SaveSettings"/>.</summary>
public sealed record EditSetting(SettingsScope Scope) : ViewEffect, IDialogEffect;