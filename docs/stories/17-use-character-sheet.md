# Use a character sheet during play

As a game master running a campaign, I want to open and update a character through the active ruleset's character-sheet definition so that the character's current state and meaningful actions are available during play.

## Outcome

A campaign character has durable character-owned state rendered through the character-sheet definition of the campaign's selected ruleset and version. Scalar values, embedded records, and rules references are presented as a coherent playable sheet rather than as a designer or raw data editor. Rollable values and actions send labelled requests to the shared dice tray.

## Acceptance criteria

- Given a configured campaign, when a character is created, then its identity records the exact ruleset ID and version whose character-sheet definition governs its data.
- Given a character-sheet definition, then its sections and fields are presented in their configured order with controls appropriate to each value kind, target type, and cardinality.
- Given editable character-owned values, when they are changed and saved, then they persist independently of published rules content and are restored when the character is reopened.
- Given an embedded-record field, then its structured character-owned values can be viewed and updated without flattening the record into display text.
- Given a rules-reference field, then compatible choices resolve by stable ruleset, version, record-type, and record key while their declarative rules content remains read-only.
- Given an unavailable or removed referenced rule, then the remaining character data stays readable and the unresolved reference is reported without silently replacing or discarding it.
- Given a value representing a current and maximum resource, then the game master can adjust its current value during play without changing its rules definition or maximum value accidentally.
- Given a rollable field or action with a valid dice request, then activating it opens the shared dice tray with a human-readable label, expression, modifier, character context, and any applicable advantage state.
- Given invalid character data or a value that no longer conforms to the pinned definition, then the sheet identifies the affected field and preserves recoverable data rather than partially saving an ambiguous state.
- Given keyboard or assistive-technology use, then sheet navigation, editing, saving, rules references, resource controls, and rollable actions are operable and meaningfully labelled.

## Not included

A generic player-facing sheet designer, multiplayer editing, derived-value scripting, automatic rules effects, automatic advancement, creation-step branching, cross-version character migration, encounter automation, or support for changing a character's governing ruleset in place.

