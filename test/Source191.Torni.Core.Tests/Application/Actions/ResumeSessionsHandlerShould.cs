using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Adapters.Claude;
using Source191.Torni.Application;
using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;
using static Source191.Torni.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Torni.Core.Tests.Application.Actions;

public sealed class ResumeSessionsHandlerShould
{
    private readonly FakeProcessRunner processes = Processes();
    private readonly FakeTranscripts transcripts = new();
    private readonly InMemoryState store = new();
    private readonly FakeWindowsTerminal terminal = new();

    private ResumeSessionsHandler Handler
    {
        get
        {
            OpenTabHandler openTab = new(store, new FakeProcessRegistry(), terminal, new FakeTimeProvider(Fixtures.Noon), new TabScripts(Claude, @"C:\Users\me\.dotnet\tools\torni.exe"));

            return new(new ClaudeCli(processes, Claude), transcripts, openTab);
        }
    }

    [Fact]
    public async Task SkipASessionWithNoTranscriptWhenResumingAndSaySo()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages(SessionId.From("sess-1"));
        Session.Background withTranscript = Job(Repository, DisplayState.Failed);
        Session.Background stub = withTranscript with { SessionId = SessionId.From("sess-stub"), Name = Listed.As("repos-f6") };

        Outcome message = await Handler.HandleAsync(new ResumeSessions([withTranscript, stub]), CancellationToken.None);

        processes.CapturedRequests.Count(request => request.Arguments.Contains("--resume")).ShouldBe(1);
        processes.CapturedRequests.Count(request => request.Arguments.Contains("rm")).ShouldBe(1, "a session with no transcript keeps its job, since it could not come back");
        ResumeSessionsOutcome resume = message.ShouldBeOfType<ResumeSessionsOutcome>();
        (resume.Resumed, resume.Chosen).ShouldBe((1, 2));
        resume.Notes.ShouldBe([new ResumeNote.NeverSentAMessage(Listed.As("repos-f6"))]);
    }

    [Fact]
    public async Task ResumeEachChosenSessionInItsOwnDirectory()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages(SessionId.From("sess-1")).WithMessages(SessionId.From("sess-2"));
        Session.Background first = Job(Repository, DisplayState.Failed);
        Session.Background second = Job(Worktree, DisplayState.Stopped) with { SessionId = SessionId.From("sess-2") };

        Outcome message = await Handler.HandleAsync(new ResumeSessions([first, second]), CancellationToken.None);

        List<ProcessRequest> resumes = [.. processes.CapturedRequests.Where(request => request.Arguments.Contains("--resume"))];
        resumes.Select(request => request.WorkingDirectory).ShouldBe([new WorkingFolder.At(Repository), new WorkingFolder.At(Worktree)]);
        resumes.Select(request => request.Arguments[2]).ShouldBe(["sess-1", "sess-2"]);
        resumes.Select(request => request.Arguments[^1]).ShouldAllBe(name => name == "fix-123", "the display name survives the resume");
        message.ShouldBeOfType<ResumeSessionsOutcome>().Resumed.ShouldBe(2);
    }

    [Fact]
    public async Task RemoveEachJobBeforeResumingIt()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages(SessionId.From("sess-1")).WithMessages(SessionId.From("sess-2"));
        Session.Background first = Job(Repository, DisplayState.Failed);
        Session.Background second = Job(Worktree, DisplayState.Stopped) with { SessionId = SessionId.From("sess-2"), JobId = JobId.From("c429a96e"), Name = Listed.As("fix-124") };

        await Handler.HandleAsync(new ResumeSessions([first, second]), CancellationToken.None);

        processes.CapturedRequests.Where(request => request.FileName == Claude).Select(request => string.Join(' ', request.Arguments)).ShouldBe(
            ["rm 81cdb0f9", "--bg --resume sess-1 -n fix-123", "rm c429a96e", "--bg --resume sess-2 -n fix-124"],
            "a listed job resumed with flags starts a copy, so it is removed first and the session id continues");
    }

    [Fact]
    public async Task NameTheSessionIdWhenAResumeFailsAfterTheRemoval()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(1, string.Empty, "Workspace not trusted"));
        transcripts.WithMessages(SessionId.From("sess-1"));

        Outcome message = await Handler.HandleAsync(new ResumeSessions([Job(Repository, DisplayState.Failed)]), CancellationToken.None);

        ResumeSessionsOutcome resume = message.ShouldBeOfType<ResumeSessionsOutcome>();
        resume.Resumed.ShouldBe(0);
        resume.Notes.ShouldBe([new ResumeNote.Failed(Listed.As("fix-123"), "Workspace not trusted", SessionId.From("sess-1"))], "the job is gone, so the note carries what brings the session back");
    }

    [Fact]
    public async Task ResumeEachSessionUnderItsListedNameOrUnnamed()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages(SessionId.From("sess-1")).WithMessages(SessionId.From("sess-2"));
        Session.Background named = Job(Repository, DisplayState.Failed);
        Session.Background blank = named with { SessionId = SessionId.From("sess-2"), Name = Listed.None() };

        await Handler.HandleAsync(new ResumeSessions([named, blank]), CancellationToken.None);

        processes.CapturedRequests.Where(request => request.Arguments.Contains("--resume")).Select(request => string.Join(" ", request.Arguments))
            .ShouldBe(["--bg --resume sess-1 -n fix-123", "--bg --resume sess-2"], "a listed name SessionName refuses is resumed without -n");
    }

    [Fact]
    public async Task OpenATabForEachSessionItResumes()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages(SessionId.From("sess-1")).WithMessages(SessionId.From("sess-2"));
        Session.Background first = Job(Repository, DisplayState.Failed);
        Session.Background second = Job(Worktree, DisplayState.Stopped) with { SessionId = SessionId.From("sess-2"), JobId = JobId.From("c429a96e"), Name = Listed.As("fix-124") };

        await Handler.HandleAsync(new ResumeSessions([first, second]), CancellationToken.None);

        terminal.Opened.Select(launch => launch.StartingDirectory.Value).ShouldBe([Repository, Worktree], "the tabs open in the order the sessions were given");
        (await store.ReadTabsAsync()).Select(tab => tab.Owner).ShouldBe([new TabOwner.ForSession(SessionId.From("sess-1")), new TabOwner.ForSession(SessionId.From("sess-2"))]);
    }

    [Fact]
    public async Task OpenNoTabForASessionThatDidNotResume()
    {
        processes.Answer(request => request.Arguments.Contains("sess-2"), new ProcessResult(1, string.Empty, "Workspace not trusted"));
        transcripts.WithMessages(SessionId.From("sess-2"));
        Session.Background stub = Job(Repository, DisplayState.Stopped) with { SessionId = SessionId.From("sess-stub"), Name = Listed.As("repos-f6") };
        Session.Background refused = Job(Worktree, DisplayState.Failed) with { SessionId = SessionId.From("sess-2"), JobId = JobId.From("c429a96e"), Name = Listed.As("fix-124") };

        Outcome message = await Handler.HandleAsync(new ResumeSessions([stub, refused]), CancellationToken.None);

        terminal.Opened.ShouldBeEmpty("neither session is running again, so there is nothing to attach to");
        (await store.ReadTabsAsync()).ShouldBeEmpty();
        message.ShouldBeOfType<ResumeSessionsOutcome>().Notes.ShouldBe([new ResumeNote.NeverSentAMessage(Listed.As("repos-f6")), new ResumeNote.Failed(Listed.As("fix-124"), "Workspace not trusted", SessionId.From("sess-2"))]);
    }

    [Fact]
    public async Task SkipASessionWhoseJobCannotBeRemovedAndResumeTheOthers()
    {
        processes.Answer(request => request.Arguments.Contains("rm") && request.Arguments.Contains("81cdb0f9"), new ProcessResult(1, string.Empty, "Session 81cdb0f9 not found"));
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · c429a96e · fix-124", string.Empty));
        transcripts.WithMessages(SessionId.From("sess-1")).WithMessages(SessionId.From("sess-2"));
        Session.Background first = Job(Repository, DisplayState.Failed);
        Session.Background second = Job(Worktree, DisplayState.Stopped) with { SessionId = SessionId.From("sess-2"), JobId = JobId.From("c429a96e"), Name = Listed.As("fix-124") };

        Outcome message = await Handler.HandleAsync(new ResumeSessions([first, second]), CancellationToken.None);

        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("sess-1"));
        terminal.Opened.ShouldHaveSingleItem().Title.ShouldBe("fix-124");
        ResumeSessionsOutcome resume = message.ShouldBeOfType<ResumeSessionsOutcome>();
        (resume.Resumed, resume.Chosen).ShouldBe((1, 2));
        resume.Notes.ShouldBe([new ResumeNote.NotRemoved(Listed.As("fix-123"), "Session 81cdb0f9 not found")]);
    }

    [Fact]
    public async Task StopAtTheFirstRefusedTab()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages(SessionId.From("sess-1")).WithMessages(SessionId.From("sess-2"));
        terminal.FailTheOpenWith = new TerminalException("wt refused");
        Session.Background first = Job(Repository, DisplayState.Failed);
        Session.Background second = Job(Worktree, DisplayState.Stopped) with { SessionId = SessionId.From("sess-2"), JobId = JobId.From("c429a96e"), Name = Listed.As("fix-124") };

        await Should.ThrowAsync<TerminalException>(() => Handler.HandleAsync(new ResumeSessions([first, second]), CancellationToken.None));

        processes.CapturedRequests.ShouldContain(request => request.Arguments.Contains("81cdb0f9") && request.Arguments.Contains("rm"));
        processes.CapturedRequests.ShouldContain(request => request.Arguments.Contains("--resume"));
        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("c429a96e"));
    }
}