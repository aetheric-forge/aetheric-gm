# Rules catalog

The Rules Catalog discovers validated, locally installed, declarative ruleset packages. A campaign stores only an optional stable `RulesetReference` consisting of an ID and version. Package acquisition is a separate concern described in [Rules package sources](rules-package-sources.md).

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
  "id": "example-fantasy",
  "version": "1.0.0",
  "name": "Example Fantasy",
  "description": "Optional human-readable catalog description.",
  "license": {
    "name": "License or proprietary-use label",
    "url": "https://example.test/license",
    "redistribution": "prohibited"
  }
}
```

IDs use lowercase kebab-case. Versions use semantic version format. Published package versions should be treated as immutable. License metadata is advisory package provenance, not a substitute for legal permission or technical rights enforcement.

## Rules records

The rules catalog provides a declarative record system for describing reusable game concepts without compiling ruleset-specific C# types into Aetheric GM. C# records implement the catalog's immutable domain objects, while ruleset packages register their own record types as data.

The model separates three things that have different ownership and lifetimes:

- a **record type** defines a reusable shape, such as `attribute`, `modifier`, `ability`, or `heritage`;
- a **rules record** is a published instance owned by the versioned ruleset, such as a particular heritage or ability;
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

For example, an `attribute` may embed repeated `modifier` records, while a `heritage` field may reference a published `heritage` rules record whose fields include flavour text and references to `ability` records. A ruleset may define a more specific concept, such as Shadowdark's `ancestry`, entirely within its external package.

### Published rules records

Rules records contain a stable key, a registered record-type key, and values conforming to that type's effective inherited fields. Their full identity is the ruleset ID, ruleset version, record-type key, and record key.

Loading is performed in two stages. JSON is first checked for structural correctness and converted to immutable domain records. Once the complete package is known, the catalog resolves type inheritance and record references. It rejects unknown properties, missing required values, invalid cardinality, incompatible value kinds, missing reference targets, duplicate identities, and inheritance cycles.

Record-reference cycles are permitted because game concepts may refer to one another, but consumers must not recursively expand such references without cycle detection.

## Catalog index

An optional `catalog.json` provides an ordered, hierarchical presentation index over published rules records. Sections and groups organize material, while a `record-catalog` item names a registered record type whose compatible records form that catalog. The index never copies rules-record content or changes record identity.

```json
{
  "sections": [
    {
      "key": "character-creation",
      "label": "Character Creation",
      "items": [
        {
          "key": "ancestries",
          "label": "Ancestries",
          "kind": "record-catalog",
          "recordType": "ancestry",
          "items": []
        }
      ]
    }
  ]
}
```

Catalog keys are stable presentation identifiers. Labels and ordering may change without invalidating rules references. Catalog documents remain declarative data and cannot contain executable behavior.

## Character-sheet definitions

The character-sheet authoring surface edits a `character-sheet.json` definition within a ruleset package. A sheet consists of ordered sections and fields. Scalar fields may be used directly; record fields select from the ruleset's registered record types.

An embedded `record` field creates character-owned state. A `rules-reference` field selects a published record without copying its rules content into the character. A `character-reference` field links to another character-owned record. This distinction keeps ruleset content immutable while allowing character state to change.

The definition describes storage and presentation shape only. Calculations, derived values, conditional fields, automatic effects, character-creation procedures, executable expressions, cross-ruleset inheritance, and arbitrary scripting remain outside this slice. Defaults may be introduced with persisted character values; until then, a missing optional value is distinct from a required value and there is no implicit default.

Persisted characters record the exact ruleset ID and version governing their sheet. A rules-reference field stores only the referenced record type and key because the character-level ruleset identity supplies the package boundary. Published labels and declarative content are resolved for display and are not copied into character-owned state.

Published ruleset versions are immutable. Changing a campaign's selected ruleset does not silently migrate existing characters; cross-version character migration remains a separate future design.
