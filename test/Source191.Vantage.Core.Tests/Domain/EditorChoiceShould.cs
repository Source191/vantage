using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class EditorChoiceShould
{
    [Fact]
    public void OfferNoVisualStudioPreset()
    {
        Enum.GetValues<EditorPreset>().ShouldBe([EditorPreset.VsCode, EditorPreset.Cursor, EditorPreset.Rider, EditorPreset.Explorer, EditorPreset.Custom], "v opens solutions in Visual Studio; e opens folders");
    }

    [Fact]
    public void DefaultToVsCode()
    {
        EditorChoice.FromFile(preset: null, executable: null, arguments: null).ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void KeepACustomEditorWithAFolderPlaceholder()
    {
        EditorChoice editor = EditorChoice.FromFile(EditorPreset.Custom, @"C:\Tools\subl.exe", ["-n", "{folder}"]);

        editor.ShouldBe(EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"]));
        editor.Executable.ShouldBe(@"C:\Tools\subl.exe");
        editor.Arguments.ShouldBe(["-n", "{folder}"]);
    }

    [Fact]
    public void ReadACustomEditorWithoutAPlaceholderAsVsCode()
    {
        EditorChoice.FromFile(EditorPreset.Custom, @"C:\Tools\subl.exe", ["-n"]).ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void ReadACustomEditorWithoutAnExecutableAsVsCode()
    {
        EditorChoice.FromFile(EditorPreset.Custom, executable: null, ["{folder}"]).ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void ReadACustomBatchFileEditorAsVsCode()
    {
        EditorChoice.FromFile(EditorPreset.Custom, @"C:\Tools\edit.cmd", ["{folder}"]).ShouldBe(EditorChoice.VsCode);
    }

    [Theory]
    [InlineData(EditorPreset.VsCode)]
    [InlineData(EditorPreset.Cursor)]
    [InlineData(EditorPreset.Rider)]
    [InlineData(EditorPreset.VisualStudio)]
    [InlineData(EditorPreset.Explorer)]
    public void KeepAPresetWithoutAnyCommand(EditorPreset preset)
    {
        EditorChoice editor = EditorChoice.FromFile(preset, @"C:\Tools\subl.exe", ["{folder}"]);

        editor.ShouldBe(EditorChoice.Of(preset));
        editor.Executable.ShouldBeEmpty();
        editor.Arguments.ShouldBeEmpty();
    }
}