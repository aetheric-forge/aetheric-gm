namespace AethericGm.Core.Characters;

public interface ICharacterRepository
{
    Task<IReadOnlyList<Character>> ListAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<Character?> GetAsync(Guid campaignId, Guid characterId, CancellationToken cancellationToken = default);
    Task SaveAsync(Character character, CancellationToken cancellationToken = default);
}
