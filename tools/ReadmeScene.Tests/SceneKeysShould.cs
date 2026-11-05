namespace Source191.Torni.ReadmeScene.Tests;

public sealed class SceneKeysShould
{
    private readonly Scene scene = SceneFile.Read(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scene.json")));

    [Fact]
    public void OpenATabForEachTabbedSession()
    {
        IReadOnlyList<KeyStep> steps = SceneKeys.OpenTabs(scene);

        string[] tabbed = [.. scene.Sessions.Where(session => session.Tab).Select(session => session.Name)];
        steps.OfType<KeyStep.Type>().Select(step => step.Text).Where(text => text.Length > 1).ShouldBe([.. tabbed, scene.Sessions.First(session => session.State == SceneState.Waiting).Name]);
        steps.OfType<KeyStep.FocusOwnTab>().Count().ShouldBe(tabbed.Length);
        steps.OfType<KeyStep.Press>().Count(step => step.Key == NamedKey.Enter).ShouldBe((tabbed.Length * 2) + 1);
    }

    [Fact]
    public void EndOnTheWaitingSessionAndMarkTheSceneReady()
    {
        IReadOnlyList<KeyStep> steps = SceneKeys.OpenTabs(scene);

        steps[^1].ShouldBeOfType<KeyStep.Mark>().Name.ShouldBe(SceneKeys.ReadyMark);
        steps[^2].ShouldBeOfType<KeyStep.Press>().Key.ShouldBe(NamedKey.Esc);
        steps[^4].ShouldBe(new KeyStep.Type(steps[^4].Delay, "fix-tax-rounding"));
    }
}