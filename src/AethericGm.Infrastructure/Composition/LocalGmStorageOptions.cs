namespace AethericGm.Infrastructure.Composition;

/// <summary>Host-owned locations; registration never depends on the current working directory.</summary>
public sealed record LocalGmStorageOptions(string DataDirectory, string RulesCatalogPath);
