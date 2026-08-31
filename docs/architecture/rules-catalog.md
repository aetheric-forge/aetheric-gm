# Rules catalog

The Rules Catalog discovers declarative, versioned ruleset packages. A campaign stores only an optional stable `RulesetReference` consisting of an ID and version.

The initial catalog reads `manifest.json` files from the configured catalog directory. Manifests contain identity and presentation metadata only. Unknown properties, invalid IDs or versions, and duplicate references are rejected when the application starts.

Manifest discovery deliberately does not:

- execute package code or scripts;
- load or interpret record types, rules records, or character-sheet definitions;
- interpret game mechanics;
- mutate campaign records; or
- make a campaign inaccessible when its referenced package is unavailable.

Those rules documents are loaded only after a package has been identified and validated. An unavailable reference is retained and shown as unavailable. This permits campaign data to remain readable when a package has been removed.

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

## Rules records

The rules catalog provides a declarative record system for describing reusable game concepts without compiling ruleset-specific C# types into Aetheric GM. C# records implement the catalog's immutable domain objects, while ruleset packages register their own record types as data.

The model separates three things that have different ownership and lifetimes:

- a **record type** defines a reusable shape, such as `attribute`, `modifier`, `ability`, or `ancestry`;
- a **rules record** is a published instance owned by the versioned ruleset, such as the `elf` ancestry or `keen-senses` ability;
- a **character record** is mutable character-owned state conforming to a record type, such as a Strength attribute and its temporary modifiers.

Record and field keys use lowercase kebab-case and are stable identifiers. Labels and descriptions are presentation content and must never be used as references.

### Record-type registry

Every ruleset version owns a registry of record-type definitions. A definition contains:

- a stable key and display label;
- an optional parent record type in the same ruleset version;
- an optional display field used to present references;
- an ordered collection of field definitions.

A child record type inherits all fields from its parent and may add fields. Initially it may not replace an inherited field with an incompatible definition. Type inheritance must be acyclic. Cross-ruleset inheritance is not supported.

Fields have an independent cardinality of `one`, `optional`, `many`, or `one-or-more`. A field's value kind is one of:

- `text`, `integer`, or `boolean` for scalar values;
- `record` for an embedded character-owned record;
- `rules-reference` for a reference to a published rules record;
- `character-reference` for a reference to another character-owned record.

Record and reference fields name the registered record type they accept. A reference accepting a parent type also accepts records of its descendant types.

For example, an `attribute` may embed repeated `modifier` records, while an `ancestry` field may reference a published `ancestry` rules record whose fields include flavour text and references to `ability` records.

### Published rules records

Rules records contain a stable key, a registered record-type key, and values conforming to that type's effective inherited fields. Their full identity is the ruleset ID, ruleset version, record-type key, and record key.

Loading is performed in two stages. JSON is first checked for structural correctness and converted to immutable domain records. Once the complete package is known, the catalog resolves type inheritance and record references. It rejects unknown properties, missing required values, invalid cardinality, incompatible value kinds, missing reference targets, duplicate identities, and inheritance cycles.

Record-reference cycles are permitted because game concepts may refer to one another, but consumers must not recursively expand such references without cycle detection.

## Character-sheet definitions

The character-sheet authoring surface edits a `character-sheet.json` definition within a ruleset package. A sheet consists of ordered sections and fields. Scalar fields may be used directly; record fields select from the ruleset's registered record types.

An embedded `record` field creates character-owned state. A `rules-reference` field selects a published record without copying its rules content into the character. A `character-reference` field links to another character-owned record. This distinction keeps ruleset content immutable while allowing character state to change.

The definition describes storage and presentation shape only. Calculations, derived values, conditional fields, automatic effects, character-creation procedures, executable expressions, cross-ruleset inheritance, and arbitrary scripting remain outside this slice. Defaults may be introduced with persisted character values; until then, a missing optional value is distinct from a required value and there is no implicit default.

Published ruleset versions are immutable. Migration of persisted character records between versions will be designed when character persistence is introduced.
