# Choose an ancestry during character creation

As a game master creating a character, I want to choose an ancestry from the active ruleset's Character Creation catalog so that the character retains a stable reference to the selected rules record.

## Outcome

A character-creation skeleton reads the campaign's selected ruleset and discovers the Ancestries record catalog under Character Creation. It presents compatible ancestry records and stores the selected rules-record identity as character-owned state conforming to the character-sheet definition.

## Acceptance criteria

- Given a campaign with an available ruleset, when character creation begins, then its Character Creation catalog is loaded from that exact ruleset ID and version.
- Given an Ancestries record catalog, then available choices are records accepted by its configured `ancestry` type and are displayed using the type's display field.
- Given a character-sheet field that is a rules reference to `ancestry`, then selecting an ancestry stores its stable ruleset, version, record-type, and record key rather than copying its label or rules text.
- Given an ancestry selection, then the chosen record's declarative content can be displayed without executing package code.
- Given a catalog reordering or label change, then an existing selection continues to resolve by stable identity.
- Given a removed or unavailable selected record, then character-owned data remains readable and the unresolved reference is reported without silently selecting a replacement.
- Given no compatible Ancestries entry, then the creation skeleton explains that the active ruleset does not currently provide ancestry choices.

## Not included

Automatic ability application, random ancestry tables, derived calculations, creation-step branching, advancement, or player-facing character creation.
