using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class EditorPresetsShould
{
    [Fact]
    public async Task AnswerThePresetsThatCannotBeFound()
    {
        FakeEditorLocator locator = new FakeEditorLocator().Install(EditorPreset.VsCode, @"C:\Users\me\AppData\Local\Programs\Microsoft VS Code\Code.exe");

        IReadOnlySet<EditorPreset> missing = await new EditorPresets(locator).MissingAsync(CancellationToken.None);

        missing.ShouldBe([EditorPreset.VisualStudio, EditorPreset.Rider], ignoreOrder: true);
    }

    [Fact]
    public async Task NeverMarkExplorerOrCustom()
    {
        IReadOnlySet<EditorPreset> missing = await new EditorPresets(new FakeEditorLocator()).MissingAsync(CancellationToken.None);

        missing.ShouldNotContain(EditorPreset.Explorer, "its answer is a path built from the Windows folder");
        missing.ShouldNotContain(EditorPreset.Custom, "Custom is the user's own executable");
    }
}