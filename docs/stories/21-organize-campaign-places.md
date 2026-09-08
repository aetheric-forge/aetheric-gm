# Organize campaign places

Milestone: [v0.3 campaign world](../product/v0.3.md)

As a game master preparing and running a campaign, I want nested locations and explicit non-containment links between them so that I can navigate the campaign's geography and connect places that aren't physically inside one another.

## Outcome

Places form a campaign-owned catalog with an explicit containment hierarchy (for example, a region containing a city containing a district) alongside separately modeled non-containment relationships, using the same typed-relationship mechanism introduced for people and factions (Story 20), for connections such as a portal, trade route, or faction control that don't imply physical nesting. People, factions, and NPCs can reference a place as their location using a stable reference rather than free text.

## Acceptance criteria

- Given a configured campaign, when the places catalog is opened, then campaign-owned places are searchable by name and browsable by their containment hierarchy.
- Given a new place, then the game master can optionally set one containing parent place; containment must remain acyclic and a place may contain many children.
- Given two places, then the game master can create a typed non-containment relationship between them using the same relationship mechanism as Story 20, independent of their containment position.
- Given a person, faction, or NPC location field, then it references a place by stable identity and resolves to that place's current name and containment path.
- Given a referenced place, when its name or containment position changes, then existing references continue to resolve by stable identity rather than by name or path.
- Given a removed place, then references to it are identified rather than silently discarded, and its former children are not deleted along with it.
- Given many places, then hierarchy browsing and search remain usable without requiring the entire tree to load at once.
- Given keyboard or assistive-technology use, then hierarchy navigation, search, editing, and relationship management are operable and meaningfully labelled.

## Not included

Maps, tactical positioning or grids, travel time or distance calculation, procedural place generation, or encounter/initiative integration.
