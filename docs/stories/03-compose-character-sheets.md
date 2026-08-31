# Compose character sheets from records

As a rules author, I want character-sheet fields to embed structured records or reference registered records so that the sheet can represent concepts richer than scalar values and string choices.

## Outcome

The character-sheet designer retains scalar fields and adds record-aware fields. After selecting a record-aware value kind, the author selects a compatible registered record type.

An Attribute field embeds character-owned state conforming to `attribute`. An Ancestry field references a published `ancestry` record, allowing its flavour text and abilities to remain part of the ruleset rather than being copied into the character.

## Acceptance criteria

- Given a scalar field, when it is edited, then text, integer, and boolean remain available.
- Given an embedded-record field, when it is edited, then the author must choose a registered record type and its future value is character-owned.
- Given a rules-reference field, when it is edited, then the author must choose a registered record type and eligible published records can be presented as choices.
- Given a character-reference field, when it is edited, then the author must choose the compatible character-owned record type.
- Given any field kind, when it is edited, then its cardinality can be selected independently.
- Given an incompatible or missing target record type, when the definition is saved or loaded, then validation fails with a useful diagnostic.
- Given a saved definition, when it is loaded again, then section order, field order, value kinds, target types, and cardinalities round-trip unchanged.
- Given the Shadowdark examples, then the designer can express a structured Strength attribute and an Ancestry selection without inline comma-separated choices.

## Not included

Character creation, character-value persistence, derived bonuses, automatic modifiers, conditional visibility, automatic ability effects, or arbitrary rules scripts.
