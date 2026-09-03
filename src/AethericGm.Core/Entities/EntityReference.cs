namespace AethericGm.Core.Entities;

public enum EntityKind { Npc, Person, Faction }

public sealed record EntityReference
{
    public EntityReference(EntityKind kind, Guid id)
    {
        Kind = kind;
        Id = id == Guid.Empty ? throw new ArgumentException("Entity ID is required.", nameof(id)) : id;
    }
    public EntityKind Kind { get; }
    public Guid Id { get; }
}
