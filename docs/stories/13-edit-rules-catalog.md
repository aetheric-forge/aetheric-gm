# Edit a local rules catalog

As a rules author, I want to edit catalog sections and their published rules records so that I can maintain a locally installed ruleset without writing code or editing JSON by hand.

## Outcome

The Rules editor presents the selected ruleset's catalog as a hierarchy. Selecting a record catalog, such as **Character Creation → Ancestries**, opens a schema-driven editor for records of its configured type. Catalog organization is saved to `catalog.json`, and published records are saved to `records.json` in the installed local package copy.

The forms are derived from `record-types.json`; no game-specific C# types or executable package code are introduced. Saving locally does not commit, push, or otherwise contribute changes to the upstream repository.

## Acceptance criteria

- Given an installed ruleset with a catalog, when it is selected in the Rules editor, then the complete catalog hierarchy is shown in its declared order.
- Given an Ancestries record catalog, when it is opened, then all compatible ancestry records are listed by their configured display field and stable key.
- Given a new record, then the editor generates its fields, value kinds, cardinalities, and reference choices from the effective registered record type.
- Given an existing record, then its stable key and typed values can be edited subject to package validation.
- Given catalog authoring, then sections, groups, and record-catalog entries can be added, renamed, reordered, and removed without copying their records into `catalog.json`.
- Given a record reference field, then the editor offers only compatible published records from the same ruleset version.
- Given invalid required values, cardinality, record types, duplicate keys, or unresolved references, then saving is rejected with field- or record-specific feedback.
- Given a successful save, then `catalog.json` and `records.json` remain declarative JSON and the editor immediately reflects the saved local state.
- Given a local edit to a Git-installed package, then its recorded upstream provenance remains visible and no Git commit, push, hook, script, or package tool is invoked.

## Not included

Upstream contribution workflows, Git commits, merge conflict handling, executable rules, automatic mechanics, or editing character-owned data.
