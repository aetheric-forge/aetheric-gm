using AethericGm.Core.Campaigns;
using AethericGm.Core.Sessions;
using AethericGm.Infrastructure.Campaigns;
using AethericGm.Infrastructure.Sessions;
using Microsoft.Data.Sqlite;

namespace AethericGm.Tests;

public sealed class SessionNotebookTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"gm-notebook-{Guid.NewGuid():N}");
    private SqliteSessionNotebookRepository repository = null!;
    private Campaign campaign = null!;
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(root);
        var connection = $"Data Source={Path.Combine(root, "test.db")}";
        var campaigns = new SqliteCampaignRepository(connection);
        await campaigns.InitializeAsync();
        campaign = Campaign.Create("The Hollow Bell", DateTimeOffset.UtcNow);
        await campaigns.SaveAsync(campaign);
        repository = new(connection);
        await repository.InitializeAsync();
        await repository.InitializeAsync();
    }
    public Task DisposeAsync() { SqliteConnection.ClearAllPools(); Directory.Delete(root, true); return Task.CompletedTask; }
    private SessionNotebook New() => new(Guid.NewGuid(), campaign.Id, "First session", "# Prep\n\nRough notes", new DateOnly(2026, 9, 8), SessionStatus.Draft, 0, DateTimeOffset.UtcNow, "operator");

    [Fact]
    public async Task Saves_lifecycle_and_restores_raw_notes_without_erasing_polished_history()
    {
        var first = await repository.SaveAsync(New());
        var live = await repository.SaveAsync(first.Edit(first.Title, "A bell rings.", first.PlannedDate, SessionStatus.Running, "operator"));
        var polished = await repository.SaveAsync(live.Edit("The Hollow Bell", "Its toll broke the silence.", live.PlannedDate, SessionStatus.Completed, "operator"));
        var raw = (await repository.GetRevisionAsync(campaign.Id, first.Id, 1))!;
        var restored = await repository.SaveAsync(polished.Edit(raw.Title, raw.Markdown, raw.PlannedDate, raw.Status, "operator"));
        Assert.Equal(4, restored.Revision);
        Assert.Equal(first.Markdown, (await repository.GetAsync(campaign.Id, first.Id))!.Markdown);
        Assert.Equal(polished.Markdown, (await repository.GetRevisionAsync(campaign.Id, first.Id, 3))!.Markdown);
        Assert.Equal(new[] { 4, 3, 2, 1 }, (await repository.ListRevisionsAsync(campaign.Id, first.Id)).Select(r => r.Revision));
        Assert.Single(await repository.ListAsync(campaign.Id));
    }
    [Fact]
    public async Task Stale_save_cannot_overwrite_newer_notes_or_append_a_revision()
    {
        var first = await repository.SaveAsync(New());
        var newer = await repository.SaveAsync(first.Edit(first.Title, "Newer notes", null, SessionStatus.Running, "operator"));
        await Assert.ThrowsAsync<SessionConflictException>(() => repository.SaveAsync(first.Edit(first.Title, "Stale notes", null, SessionStatus.Draft, "operator")));
        Assert.Equal(newer.Markdown, (await repository.GetAsync(campaign.Id, first.Id))!.Markdown);
        Assert.Equal(2, (await repository.ListRevisionsAsync(campaign.Id, first.Id)).Count);
    }
    [Fact]
    public async Task Campaign_boundary_applies_to_head_history_and_writes()
    {
        var saved = await repository.SaveAsync(New());
        var other = Guid.NewGuid();
        Assert.Null(await repository.GetAsync(other, saved.Id));
        Assert.Null(await repository.GetRevisionAsync(other, saved.Id, 1));
        Assert.Empty(await repository.ListRevisionsAsync(other, saved.Id));
        var forged = new SessionNotebook(saved.Id, other, saved.Title, "Wrong campaign", null, SessionStatus.Draft, saved.Revision, saved.UpdatedAt, "operator");
        await Assert.ThrowsAsync<SessionConflictException>(() => repository.SaveAsync(forged));
        Assert.Equal(saved.Markdown, (await repository.GetAsync(campaign.Id, saved.Id))!.Markdown);
    }
    [Fact]
    public async Task Cannot_create_notebook_for_nonexistent_campaign()
    {
        var value = New();
        await Assert.ThrowsAsync<SqliteException>(() => repository.SaveAsync(new(value.Id, Guid.NewGuid(), value.Title, value.Markdown, null, value.Status, 0, value.UpdatedAt, "operator")));
        Assert.Empty(await repository.ListRevisionsAsync(campaign.Id, value.Id));
    }
    [Fact]
    public async Task Duplicate_creation_is_a_conflict_not_an_overwrite()
    {
        var value = New();
        await repository.SaveAsync(value);
        await Assert.ThrowsAsync<SessionConflictException>(() => repository.SaveAsync(value));
        Assert.Single(await repository.ListRevisionsAsync(campaign.Id, value.Id));
    }
    [Fact]
    public async Task Continuation_copies_only_chosen_notes_and_keeps_source_revision_after_edits()
    {
        var source = await repository.SaveAsync(New());
        var next = await repository.SaveAsync(source.CreateNext("Second session", "Unresolved: find the bell.", null, "operator"));
        Assert.NotEqual(source.Id, next.Id);
        Assert.Equal(SessionStatus.Draft, next.Status);
        Assert.Equal(source.Id, next.PreviousSessionId);
        Assert.Equal(source.Revision, next.SourceRevision);
        Assert.Equal("Unresolved: find the bell.", next.Markdown);
        Assert.Equal(source, await repository.GetAsync(campaign.Id, source.Id));
        await repository.SaveAsync(source.Edit("Renamed source", "Polished later", null, SessionStatus.Completed, "operator"));
        var reopened = (await repository.GetAsync(campaign.Id, next.Id))!;
        Assert.Equal(source.Markdown, (await repository.GetRevisionAsync(campaign.Id, reopened.PreviousSessionId!.Value, reopened.SourceRevision!.Value))!.Markdown);
        var edited = await repository.SaveAsync(reopened.Edit("Second session edited", "More notes", null, SessionStatus.Running, "operator"));
        Assert.Equal(source.Id, edited.PreviousSessionId);
        Assert.Equal(source.Id, (await repository.GetRevisionAsync(campaign.Id, next.Id, 2))!.PreviousSessionId);
    }

    [Fact]
    public async Task Continuation_allows_blank_notes_and_multiple_following_sessions()
    {
        var source = await repository.SaveAsync(New());
        var first = await repository.SaveAsync(source.CreateNext("Next", "", null, "operator"));
        await repository.SaveAsync(source.CreateNext("Alternative", "Other thread", null, "operator"));
        Assert.Empty(first.Markdown);
        Assert.Equal(2, (await repository.ListAsync(campaign.Id)).Count(n => n.PreviousSessionId == source.Id));
    }

    [Fact]
    public async Task Source_cannot_be_missing_cross_campaign_or_retargeted()
    {
        var source = await repository.SaveAsync(New());
        var next = source.CreateNext("Next", "Notes", null, "operator");
        var otherCampaign = Guid.NewGuid();
        await Assert.ThrowsAsync<ArgumentException>(() => repository.SaveAsync(new SessionNotebook(next.Id, otherCampaign, next.Title, next.Markdown,
            null, next.Status, 0, next.UpdatedAt, "operator", source.Id, 1)));
        await Assert.ThrowsAsync<ArgumentException>(() => repository.SaveAsync(new SessionNotebook(next.Id, campaign.Id, next.Title, next.Markdown,
            null, next.Status, 0, next.UpdatedAt, "operator", source.Id, 999)));
        var saved = await repository.SaveAsync(next);
        var unrelated = await repository.SaveAsync(New());
        await Assert.ThrowsAsync<SessionConflictException>(() => repository.SaveAsync(new SessionNotebook(saved.Id, campaign.Id, saved.Title, saved.Markdown,
            null, saved.Status, saved.Revision, saved.UpdatedAt, "operator", unrelated.Id, unrelated.Revision)));
        await Assert.ThrowsAsync<SessionConflictException>(() => repository.SaveAsync(new SessionNotebook(saved.Id, campaign.Id, saved.Title, saved.Markdown,
            null, saved.Status, saved.Revision, saved.UpdatedAt, "operator")));
        Assert.Single(await repository.ListRevisionsAsync(campaign.Id, saved.Id));
    }

    [Fact]
    public void Unsaved_notebooks_cannot_be_continued()
    {
        Assert.Throws<InvalidOperationException>(() => New().CreateNext("Next", "Notes", null, "operator"));
        var value = New();
        Assert.Throws<ArgumentException>(() => new SessionNotebook(value.Id, campaign.Id, "Title", "", null, SessionStatus.Draft, 0,
            value.UpdatedAt, "operator", value.Id, 1));
    }

    [Fact]
    public async Task Upgrades_existing_notebooks_and_history_without_changing_their_content()
    {
        var connectionString = $"Data Source={Path.Combine(root, "old.db")}";
        var campaigns = new SqliteCampaignRepository(connectionString);
        await campaigns.InitializeAsync(); await campaigns.SaveAsync(campaign);
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE session_schema_versions(version INTEGER PRIMARY KEY);
                INSERT INTO session_schema_versions VALUES(1);
                CREATE TABLE session_notebooks(id TEXT PRIMARY KEY,campaign_id TEXT NOT NULL,title TEXT NOT NULL,markdown TEXT NOT NULL,
                    planned_date TEXT,status INTEGER NOT NULL,revision INTEGER NOT NULL,updated_at TEXT NOT NULL,author TEXT NOT NULL);
                CREATE TABLE session_notebook_revisions(id TEXT NOT NULL,campaign_id TEXT NOT NULL,title TEXT NOT NULL,markdown TEXT NOT NULL,
                    planned_date TEXT,status INTEGER NOT NULL,revision INTEGER NOT NULL,updated_at TEXT NOT NULL,author TEXT NOT NULL,PRIMARY KEY(id,revision));
                INSERT INTO session_notebooks VALUES($id,$campaign,'Existing','Original notes',NULL,0,1,$now,'operator');
                INSERT INTO session_notebook_revisions SELECT * FROM session_notebooks;
                """;
            command.Parameters.AddWithValue("$id", campaign.Id.ToString());
            command.Parameters.AddWithValue("$campaign", campaign.Id.ToString());
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync();
        }
        var upgraded = new SqliteSessionNotebookRepository(connectionString);
        await upgraded.InitializeAsync(); await upgraded.InitializeAsync();
        var existing = (await upgraded.GetAsync(campaign.Id, campaign.Id))!;
        Assert.Null(existing.PreviousSessionId);
        Assert.Equal("Original notes", existing.Markdown);
        Assert.Equal(existing, await upgraded.GetRevisionAsync(campaign.Id, existing.Id, 1));
        var next = await upgraded.SaveAsync(existing.CreateNext("Next", "Keep", null, "operator"));
        Assert.Equal(existing.Id, next.PreviousSessionId);
    }

    [Fact]
    public void Rejects_invalid_state_and_preserves_markdown_whitespace()
    {
        var value = New();
        Assert.Throws<ArgumentException>(() => value.Edit(" ", "text", null, SessionStatus.Draft, "operator"));
        Assert.Throws<ArgumentException>(() => value.Edit("Title", "text", null, (SessionStatus)99, "operator"));
        Assert.Throws<ArgumentException>(() => value.Edit("Title", "text", null, SessionStatus.Draft, ""));
        Assert.Equal("  prose\n\n", value.Edit("Title", "  prose\n\n", null, SessionStatus.Draft, "operator").Markdown);
    }
}
