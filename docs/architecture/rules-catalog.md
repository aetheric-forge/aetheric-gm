# Rules catalog

The Rules Catalog discovers declarative, versioned ruleset packages. A campaign stores only an optional stable `RulesetReference` consisting of an ID and version.

The initial catalog reads `manifest.json` files from the configured catalog directory. Manifests contain identity and presentation metadata only. Unknown properties, invalid IDs or versions, and duplicate references are rejected when the application starts.

The catalog deliberately does not:

- execute package code or scripts;
- define character sheets;
- interpret game mechanics;
- mutate campaign records; or
- make a campaign inaccessible when its referenced package is unavailable.

An unavailable reference is retained and shown as unavailable. This permits campaign data to remain readable when a package has been removed.

## Manifest format

```json
{
  "id": "shadowdark",
  "version": "1.0.0",
  "name": "Shadowdark RPG",
  "description": "Optional human-readable catalog description."
}
```

IDs use lowercase kebab-case. Versions use semantic version format. Published package versions should be treated as immutable.
