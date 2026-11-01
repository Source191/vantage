using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class EditorChoiceShould
{
    [Fact]
    public void OfferVisualStudioInPlaceOfCursor()
    {
        Enum.GetValues<EditorPreset>().ShouldBe([EditorPreset.VsCode, EditorPreset.VisualStudio, EditorPreset.Rider, EditorPreset.Explorer, EditorPreset.Custom], "the owner's choice of 31 October 2026");
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
    public void ReadACustomEditorWithNoArgumentsAsVsCode()
    {
        EditorChoice.FromFile(EditorPreset.Custom, @"C:\Tools\subl.exe", arguments: null).ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void ReadACustomEditorWithoutAnExecutableAsVsCode()
    {
        EditorChoice.FromFile(EditorPreset.Custom, executable: null, ["{folder}"]).ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void ReadACustomEditorByBareNameAsVsCode()
    {
        EditorChoice.FromFile(EditorPreset.Custom, "subl", ["{folder}"]).ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void ReadACustomBatchFileEditorAsVsCode()
    {
        EditorChoice.FromFile(EditorPreset.Custom, @"C:\Tools\edit.cmd", ["{folder}"]).ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void RefuseACustomCommandWithTheFirstProblem()
    {
        EditorChoice.Validate("  ", ["-n"]).ShouldBe(new EditorChoiceValidation.Refused(new SettingsProblem.NoExecutable()));
        EditorChoice.Validate("subl.cmd", ["-n"]).ShouldBe(new EditorChoiceValidation.Refused(new SettingsProblem.ExecutableNotFullPath()));
        EditorChoice.Validate(@"C:\Tools\edit.cmd", ["-n"]).ShouldBe(new EditorChoiceValidation.Refused(new SettingsProblem.BatchFileExecutable()));
        EditorChoice.Validate(@"C:\Tools\subl.exe", ["-n"]).ShouldBe(new EditorChoiceValidation.Refused(new SettingsProblem.NoFolderPlaceholder()));
    }

    [Fact]
    public void AcceptACustomCommandAsValid()
    {
        EditorChoice editor = EditorChoice.Validate(@" C:\Tools\subl.exe ", ["-n", "{folder}"]).ShouldBeOfType<EditorChoiceValidation.Valid>().Editor;

        editor.Preset.ShouldBe(EditorPreset.Custom);
        editor.Executable.ShouldBe(@"C:\Tools\subl.exe");
        editor.Arguments.ShouldBe(["-n", "{folder}"]);
    }

    [Theory]
    [InlineData(EditorPreset.VsCode)]
    [InlineData(EditorPreset.VisualStudio)]
    [InlineData(EditorPreset.Rider)]
    [InlineData(EditorPreset.Explorer)]
    public void KeepAPresetWithoutAnyCommand(EditorPreset preset)
    {
        EditorChoice editor = EditorChoice.FromFile(preset, @"C:\Tools\subl.exe", ["{folder}"]);

        editor.ShouldBe(EditorChoice.Of(preset));
        editor.Executable.ShouldBeEmpty();
        editor.Arguments.ShouldBeEmpty();
    }
}