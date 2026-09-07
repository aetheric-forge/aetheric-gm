# Organize rules content in a catalog

As a rules author, I want to organize a ruleset into named hierarchical sections so that related material is discoverable without duplicating its underlying rules records.

## Outcome

A ruleset package may contain a declarative `catalog.json` presentation index. Catalog sections and groups form a hierarchy, while record-catalog entries point to registered record types. The first useful structure is a **Character Creation** section containing an **Ancestries** catalog backed by the ruleset's `ancestry` records.

The hierarchy organizes rules for authoring and consumption; it does not change record identity, copy records into the tree, or make the ruleset domain intrinsically hierarchical.

## Acceptance criteria

- Given a valid catalog document, when the package is loaded, then its ordered sections, groups, and record-catalog entries are available by stable key and display label.
- Given a record-catalog entry, then it identifies one registered record type in the same ruleset version.
- Given a Character Creation section containing an Ancestries entry, then the entry exposes all published records accepted by the configured `ancestry` record type.
- Given nested groups, then their order and containment are retained for presentation without changing the identity or ownership of referenced records.
- Given duplicate sibling keys, an unknown record type, an unsupported item kind, or malformed nesting, then package validation fails with a location-specific diagnostic.
- Given labels or ordering that change, then stable keys and rules-record references remain unchanged.
- Given a package without `catalog.json`, then its existing manifest, record types, records, and character-sheet definition continue to load.
- Given any catalog document, then loading it does not execute expressions, scripts, package tooling, or arbitrary code.

## Not included

Editing catalog contents, character persistence, creation procedures, automatic effects, executable conditions, or cross-ruleset catalog entries.
