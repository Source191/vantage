namespace Source191.Vantage.Application.Views;

/// <summary>The fact behind a table's title: <see cref="Noun"/> is singular (session), <see cref="Filter"/> empty when none is set and <see cref="Selected"/> null when no row is.</summary>
public sealed record TableTitle(string Noun, int Shown, int Total, string Filter, string? Selected);