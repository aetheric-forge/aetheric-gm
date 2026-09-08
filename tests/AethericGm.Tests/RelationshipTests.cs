using AethericGm.Core.Entities;
using AethericGm.Core.Relationships;

namespace AethericGm.Tests;

public class RelationshipTests
{
    [Fact]
    public void Rejects_connecting_an_entity_to_itself()
    {
        var subject = new EntityReference(EntityKind.Person, Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => Relationship.Create(Guid.NewGuid(), subject, subject, "self", false, false, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Update_mutates_label_symmetry_and_secrecy()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var relationship = Relationship.Create(Guid.NewGuid(), new EntityReference(EntityKind.Person, Guid.NewGuid()),
            new EntityReference(EntityKind.Faction, Guid.NewGuid()), "reports to", false, false, now);

        relationship.Update("rival of", true, true, now.AddHours(1));

        Assert.Equal("rival of", relationship.Label);
        Assert.True(relationship.IsSymmetric);
        Assert.True(relationship.IsSecret);
        Assert.Equal(now.AddHours(1), relationship.UpdatedAt);
    }

    [Fact]
    public void Involves_matches_either_endpoint_only()
    {
        var from = new EntityReference(EntityKind.Npc, Guid.NewGuid());
        var to = new EntityReference(EntityKind.Person, Guid.NewGuid());
        var relationship = Relationship.Create(Guid.NewGuid(), from, to, "reports to", false, false, DateTimeOffset.UtcNow);

        Assert.True(relationship.Involves(from));
        Assert.True(relationship.Involves(to));
        Assert.False(relationship.Involves(new EntityReference(EntityKind.Faction, Guid.NewGuid())));
    }
}
