namespace AethericGm.Core.Sessions;

public interface ISessionNotebookRepository
{
    Task<IReadOnlyList<SessionNotebook>> ListAsync(Guid campaignId, CancellationToken ct = default);
    Task<SessionNotebook?> GetAsync(Guid campaignId, Guid sessionId, CancellationToken ct = default);
    // Atomically compare the supplied Revision, save the new head, and append its immutable history.
    Task<SessionNotebook> SaveAsync(SessionNotebook notebook, CancellationToken ct = default);
    Task<IReadOnlyList<SessionRevisionSummary>> ListRevisionsAsync(Guid campaignId, Guid sessionId, CancellationToken ct = default);
    Task<SessionNotebook?> GetRevisionAsync(Guid campaignId, Guid sessionId, int revision, CancellationToken ct = default);
}
