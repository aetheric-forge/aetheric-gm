# Use a character sheet during play

As a game master running a campaign, I want to open and update a character through the active ruleset's character-sheet definition so that the character's current state and meaningful actions are available during play.

## Outcome

A campaign character has durable character-owned state rendered through the character-sheet definition of the ruleset and version recorded when that character was created. Scalar values, embedded records, and rules references are presented as a coherent sheet rather than as a rules designer or raw JSON editor.

The first proving sheet reopens the character created in Story 15, showing its name and ancestry. The ancestry's flavour text and referenced abilities, such as Farsight or Ambitious, are resolved from published rules content and remain read-only. Supported inline roll tokens in displayed rules prose reuse the shared dice result experience.

## Acceptance criteria

- Given a campaign character, when its sheet is opened, then the definition is loaded from the exact ruleset ID and version recorded by that character rather than whichever ruleset is newest or currently selected elsewhere.
- Given a character-sheet definition, then its sections and fields are presented in their configured order with controls appropriate to each value kind, target type, and cardinality.
- Given editable character-owned values, when they are changed and saved, then they persist independently of published rules content and are restored when the character is reopened.
- Given an embedded-record field, then its structured character-owned values can be viewed and updated without flattening the record into display text.
- Given a rules-reference field, then compatible choices resolve by stable ruleset, version, record-type, and record key while their declarative rules content remains read-only.
- Given the ancestry rules reference, then the sheet displays resolvable ancestry fields and follows its ability references for read-only display without copying or automatically applying those abilities to character state.
- Given an unavailable or removed referenced rule, then the remaining character data stays readable and the unresolved reference is reported without silently replacing or discarding it.
- Given valid inline roll syntax in displayed rules prose, then activating it uses the shared dice result presentation with a human-readable label and character/rules context.
- Given an inline roll whose expression requires a character value, then it is enabled only when that value can be resolved from the character's stored fields; an unresolved value is explained rather than replaced with zero.
- Given invalid character data or a value that no longer conforms to the pinned definition, then the sheet identifies the affected field and preserves recoverable data rather than partially saving an ambiguous state.
- Given keyboard or assistive-technology use, then sheet navigation, editing, saving, rules references, and inline rolls are operable and meaningfully labelled.

## Not included

A generic player-facing sheet designer, multiplayer editing, derived-value scripting, automatic rules effects or ability grants, dedicated action metadata, current/maximum resource semantics, automatic advancement, creation-step branching, cross-version character migration, encounter automation, or support for changing a character's governing ruleset in place.
