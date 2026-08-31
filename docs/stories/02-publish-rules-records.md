# Publish rules records

As a rules author, I want to publish records conforming to registered record types so that character sheets can refer to stable rules content such as ancestries and abilities.

## Outcome

A versioned ruleset can contain immutable rules records. Each record has a stable identity and typed values validated against its registered record type.

For Shadowdark, an ancestry such as `elf` can contain its name and flavour text and refer to one or more separately published ability records.

## Acceptance criteria

- Given a valid rules-record document, when the package is loaded, then the record is constructed as an immutable domain object.
- Given a rules record, then its identity includes ruleset ID, ruleset version, record-type key, and record key.
- Given inherited fields, when a record is validated, then it is checked against the complete effective record type.
- Given `one`, `optional`, `many`, or `one-or-more` cardinality, when values are loaded, then missing and repeated values are validated accordingly.
- Given a rules reference, when the complete package has loaded, then its target must exist and have a compatible type.
- Given an unknown value, incorrect scalar kind, duplicate identity, missing required value, or unresolved reference, then package loading fails with a useful diagnostic.
- Given cyclic rules references, then the records may load successfully and consumers can detect cycles while traversing them.
- Given a record type with a display field, then authoring tools can obtain a stable human-readable label for its records without using that label as identity.

## Not included

Editing published rules content from a character sheet, copying flavour text into character data, automatic application of abilities, or migration between ruleset versions.
