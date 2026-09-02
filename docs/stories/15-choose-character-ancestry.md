# Create a character with an ancestry

As a game master, I want to create a named character and choose an ancestry from the campaign's pinned ruleset so that I can reopen a valid character sheet with its rules references intact.

## Outcome

A campaign provides a small creation form governed by its pinned character-sheet definition. The first proving path captures the sheet's `name` text field and discovers compatible choices for its `ancestry` rules-reference field from **Character Creation → Ancestries**. Saving creates durable campaign-owned character state and opens the resulting sheet.

The ancestry value stores a stable reference; its label, flavour text, and referenced abilities remain published rules content. They are resolved for display rather than copied into character data or applied as executable effects.

## Acceptance criteria

- Given a configured campaign, when character creation begins, then the character-sheet definition and Character Creation catalog are loaded from the campaign's exact pinned ruleset ID and version.
- Given the proving character-sheet definition, then creation presents its required `name` text field and its `ancestry` rules-reference field without hard-coding Shadowdark fields into the domain model.
- Given an Ancestries record catalog, then available choices are records accepted by its configured `ancestry` type and are displayed using that type's display field.
- Given a selected ancestry, then the character stores its governing ruleset ID and version once and stores the ancestry's record type and stable key as the field value; it does not copy the ancestry label, flavour text, or abilities.
- Given valid creation values, when the game master saves, then a durable campaign-owned character is created, appears in that campaign, and can be reopened as a character sheet.
- Given an ancestry selection, then its declarative fields and resolvable ability references can be displayed read-only without executing package code or duplicating those abilities as character-owned values.
- Given a catalog reordering or label change, then an existing selection continues to resolve by stable identity.
- Given a removed or unavailable selected record, then character-owned data remains readable and the unresolved reference is reported without silently selecting a replacement.
- Given a missing character-sheet definition, unavailable pinned ruleset, or no compatible Ancestries entry, then creation is blocked with a specific explanation and no partial character is saved.
- Given invalid required values, then the affected field is identified and no partial character is saved.

## Not included

Automatic ability application, choosing abilities independently of their ancestry, random ancestry tables, attribute generation, derived calculations, creation-step branching, advancement, or player-facing character creation.
