namespace Source191.Torni.ReadmeScene;

/// <summary>
/// The keys that set the scene up once the console is showing: for each session with a tab, filter to it, open its tab
/// with Enter and come back to Torni's tab; then select the waiting session, so the detail pane shows what it waits
/// for, and mark the scene ready for the screenshot.
/// </summary>
public static class SceneKeys
{
    public const string ReadyMark = "ready";

    private static readonly TimeSpan FirstListing = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Beat = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan TabOpening = TimeSpan.FromMilliseconds(3500);
    private static readonly TimeSpan Settling = TimeSpan.FromSeconds(3);

    public static IReadOnlyList<KeyStep> OpenTabs(Scene scene)
    {
        SceneSession selected = scene.Sessions.FirstOrDefault(session => session.State == SceneState.Waiting) ?? scene.Sessions[0];
        List<KeyStep> steps = [new KeyStep.Type(FirstListing, string.Empty)];
        foreach (SceneSession session in scene.Sessions.Where(session => session.Tab))
        {
            steps.AddRange(FilterTo(session.Name));
            steps.Add(new KeyStep.Press(Beat, NamedKey.Enter));
            steps.Add(new KeyStep.FocusOwnTab(TabOpening));
            steps.Add(new KeyStep.Press(Beat, NamedKey.Esc));
        }

        steps.AddRange(FilterTo(selected.Name));
        steps.Add(new KeyStep.Press(Beat, NamedKey.Esc));
        steps.Add(new KeyStep.Mark(Settling, ReadyMark));

        return steps;
    }

    private static KeyStep[] FilterTo(string name) =>
    [
        new KeyStep.Type(Beat, "/"),
        new KeyStep.Type(Beat, name),
        new KeyStep.Press(Beat, NamedKey.Enter),
    ];
}