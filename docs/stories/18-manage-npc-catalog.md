# Manage an NPC catalog

As a game master preparing and running a campaign, I want to find, create, and use NPCs from a campaign catalog so that recurring people and ready-to-run adversaries are available without searching through notes or rules books.

## Outcome

The campaign provides a searchable catalog of campaign-owned NPCs alongside compatible NPC records supplied by its pinned ruleset. A rules NPC can be inspected or copied into the campaign for customization without modifying the installed package. Campaign NPCs retain their own identity, notes, current state, and rules references, and expose rollable actions through the shared dice tray.

## Acceptance criteria

- Given a configured campaign, when its NPC catalog is opened, then campaign-owned NPCs and compatible NPC records from the exact selected ruleset ID and version are clearly distinguished.
- Given a catalog containing many NPCs, then the game master can find entries by name and filter them by available source, tag, role, location, or status without losing the distinction between package and campaign ownership.
- Given an NPC entry, then its game-facing summary, defenses, resources, traits, actions, notes, tags, disposition, location, and status are shown when those values are available.
- Given a package-owned NPC, then the game master can inspect its declarative content but cannot accidentally edit the installed rules record as campaign state.
- Given a compatible package-owned NPC, when it is added to the campaign, then a new campaign-owned NPC is created with a stable identity and an explicit reference to its source rules record rather than becoming a mutable package record.
- Given a new or copied campaign NPC, then the game master can edit its campaign-owned name, notes, tags, disposition, location, status, and supported game state without changing the source rules package.
- Given a campaign NPC created from a rules record, then later package label or ordering changes do not silently change its campaign identity or overwrite its campaign-owned state.
- Given an unavailable source rules record, then the NPC's campaign-owned data remains readable and its unresolved source is reported without deleting or replacing the NPC.
- Given a rollable NPC action with a valid dice request, then activating it uses the shared dice tray with a human-readable label, expression, modifier, NPC context, and any applicable advantage state.
- Given multiple NPCs based on the same source record, then each retains independent current resources, status, notes, and roll context.
- Given keyboard or assistive-technology use, then catalog search, filters, entry inspection, editing, and rollable actions are operable and meaningfully labelled.

## Not included

Encounter composition, initiative, automated combat, tactical maps, procedural NPC generation, bulk package-to-campaign copying, cross-version migration, multiplayer ownership, or player-facing NPC views.

