namespace AethericGm.Core.Sessions;

public enum SessionStatus { Draft, Running, Completed }

/// <summary>An immutable notebook snapshot. Revision zero denotes a new, unsaved session.</summary>
public sealed record SessionNotebook
{
    public Guid Id { get; }
    public Guid CampaignId { get; }
    public string Title { get; }
    public string Markdown { get; }
    public DateOnly? PlannedDate { get; }
    public SessionStatus Status { get; }
    public int Revision { get; }
    public DateTimeOffset UpdatedAt { get; }
    public string AuthorSubjectId { get; }

    public SessionNotebook(Guid id, Guid campaignId, string title, string markdown, DateOnly? plannedDate,
        SessionStatus status, int revision, DateTimeOffset updatedAt, string authorSubjectId)
    {
        if (id == Guid.Empty || campaignId == Guid.Empty) throw new ArgumentException("Session and campaign IDs are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (title.Trim().Length > 200) throw new ArgumentException("Session title cannot exceed 200 characters.");
        ArgumentNullException.ThrowIfNull(markdown);
        if (markdown.Length > 1_000_000) throw new ArgumentException("Notebook cannot exceed one million characters.");
        ArgumentException.ThrowIfNullOrWhiteSpace(authorSubjectId);
        if (!Enum.IsDefined(status) || revision < 0) throw new ArgumentException("Invalid session state.");
        Id = id; CampaignId = campaignId; Title = title.Trim(); Markdown = markdown; PlannedDate = plannedDate;
        Status = status; Revision = revision; UpdatedAt = updatedAt; AuthorSubjectId = authorSubjectId;
    }

    public SessionNotebook Edit(string title, string markdown, DateOnly? plannedDate, SessionStatus status, string author) =>
        new(Id, CampaignId, title, markdown, plannedDate, status, Revision, UpdatedAt, author);
}

public sealed record SessionRevisionSummary(int Revision, DateTimeOffset SavedAt, string AuthorSubjectId, string Title, SessionStatus Status);

public sealed class SessionConflictException() : InvalidOperationException("This notebook changed in another tab. Your draft has been kept; reload the latest version before saving again.");
