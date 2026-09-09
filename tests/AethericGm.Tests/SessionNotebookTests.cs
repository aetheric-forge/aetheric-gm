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
    public void Rejects_invalid_state_and_preserves_markdown_whitespace()
    {
        var value = New();
        Assert.Throws<ArgumentException>(() => value.Edit(" ", "text", null, SessionStatus.Draft, "operator"));
        Assert.Throws<ArgumentException>(() => value.Edit("Title", "text", null, (SessionStatus)99, "operator"));
        Assert.Throws<ArgumentException>(() => value.Edit("Title", "text", null, SessionStatus.Draft, ""));
        Assert.Equal("  prose\n\n", value.Edit("Title", "  prose\n\n", null, SessionStatus.Draft, "operator").Markdown);
    }
}
