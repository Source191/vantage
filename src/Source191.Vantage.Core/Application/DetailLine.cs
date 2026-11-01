namespace Source191.Vantage.Application;

/// <summary>One fact of the detail pane: a label in its column's label width and its text, in a tone, across one or more of the pane's three columns.</summary>
public sealed record DetailLine(string Label, string Text, TextTone Tone = TextTone.Plain, int Span = 1);