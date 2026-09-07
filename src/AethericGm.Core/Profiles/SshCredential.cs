namespace AethericGm.Core.Profiles;

public sealed record SshCredential
{
    public SshCredential(Guid id, string ownerSubjectId, string name, string algorithm, string fingerprint, bool requiresPassphrase, DateTimeOffset createdAt, DateTimeOffset? lastUsedAt = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Credential ID is required.", nameof(id));
        Id = id;
        OwnerSubjectId = Required(ownerSubjectId, nameof(ownerSubjectId), 255);
        Name = NormalizeName(name);
        Algorithm = Required(algorithm, nameof(algorithm), 100);
        Fingerprint = Required(fingerprint, nameof(fingerprint), 200);
        RequiresPassphrase = requiresPassphrase;
        CreatedAt = createdAt;
        LastUsedAt = lastUsedAt;
    }

    public Guid Id { get; }
    public string OwnerSubjectId { get; }
    public string Name { get; }
    public string Algorithm { get; }
    public string Fingerprint { get; }
    public bool RequiresPassphrase { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? LastUsedAt { get; }

    public static string NormalizeName(string name) => Required(name, nameof(name), 100);

    private static string Required(string value, string parameter, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", parameter);
        var trimmed = value.Trim();
        return trimmed.Length <= maximumLength ? trimmed : throw new ArgumentException($"Value cannot exceed {maximumLength} characters.", parameter);
    }
}
