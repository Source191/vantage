using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class OpenInEditorHandlerShould
{
    private const string Orders = @"C:\Users\me\source\repos\Northwind.Orders";

    private readonly FakeFolders folders = new FakeFolders().WithDirectory(Orders);
    private readonly FakeDesktop desktop = new();
    private readonly FakeEditorLocator locator = new FakeEditorLocator().Install(EditorPreset.VsCode, @"C:\Code\Code.exe");

    [Fact]
    public async Task StartTheChosenEditorOnTheFolder()
    {
        OpenInEditorHandler handler = new(locator, desktop, folders, CurrentWith(EditorChoice.VsCode));

        Outcome outcome = await handler.HandleAsync(new OpenFolderInEditor(FolderPath.From(Orders)), CancellationToken.None);

        desktop.Started.Single().FileName.ShouldBe(@"C:\Code\Code.exe");
        desktop.Started.Single().Arguments.ShouldBe([Orders]);
        outcome.ShouldBe(new OpenInEditorOutcome.Opened("Northwind.Orders", EditorChoice.VsCode));
    }

    [Fact]
    public async Task StartACustomEditorFromItsOwnExecutable()
    {
        EditorChoice custom = EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"]);
        OpenInEditorHandler handler = new(locator, desktop, folders.WithFile(@"C:\Tools\subl.exe"), CurrentWith(custom));

        Outcome outcome = await handler.HandleAsync(new OpenFolderInEditor(FolderPath.From(Orders)), CancellationToken.None);

        desktop.Started.Single().FileName.ShouldBe(@"C:\Tools\subl.exe");
        desktop.Started.Single().Arguments.ShouldBe(["-n", Orders]);
        outcome.ShouldBe(new OpenInEditorOutcome.Opened("Northwind.Orders", custom));
    }

    [Fact]
    public async Task ReportAnEditorThatIsNotInstalled()
    {
        EditorChoice visualStudio = EditorChoice.Of(EditorPreset.VisualStudio);
        OpenInEditorHandler handler = new(locator, desktop, folders, CurrentWith(visualStudio));

        Outcome outcome = await handler.HandleAsync(new OpenFolderInEditor(FolderPath.From(Orders)), CancellationToken.None);

        desktop.Started.ShouldBeEmpty();
        outcome.ShouldBe(new OpenInEditorOutcome.EditorNotFound(visualStudio));
    }

    [Fact]
    public async Task ReportACustomEditorWhoseExecutableIsMissing()
    {
        EditorChoice custom = EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"]);
        OpenInEditorHandler handler = new(locator, desktop, folders, CurrentWith(custom));

        Outcome outcome = await handler.HandleAsync(new OpenFolderInEditor(FolderPath.From(Orders)), CancellationToken.None);

        desktop.Started.ShouldBeEmpty();
        outcome.ShouldBe(new OpenInEditorOutcome.EditorNotFound(custom));
    }

    [Fact]
    public async Task ReportAMissingFolder()
    {
        OpenInEditorHandler handler = new(locator, desktop, folders, CurrentWith(EditorChoice.VsCode));

        Outcome outcome = await handler.HandleAsync(new OpenFolderInEditor(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Gone")), CancellationToken.None);

        desktop.Started.ShouldBeEmpty();
        outcome.ShouldBe(new OpenInEditorOutcome.FolderMissing("Northwind.Gone"));
    }

    private static CurrentSettings CurrentWith(EditorChoice editor)
    {
        return TestSettings.Current(Source191.Vantage.Domain.Settings.Resolve([FolderPath.From(@"C:\Users\me\source\repos")], new WorktreeRootChoice.BesideFirstRoot(), editor, WorktreeBase.RemoteDefault, TestSettings.Home));
    }
}