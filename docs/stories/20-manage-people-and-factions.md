# Manage people and factions

Milestone: [v0.3 campaign world](../product/v0.3.md)

As a game master preparing and running a campaign, I want a searchable catalog of people and factions with typed relationships and tags so that I can track who matters, how they connect, and what stays hidden from players without losing track of it.

## Outcome

The campaign catalog extends beyond statted NPCs (Story 18) to any named person or faction worth tracking — contacts, factions, organizations, and other groups — sharing the same campaign-owned identity and tagging model. Any two catalog entries, including NPCs, can be linked through an explicit typed relationship (such as "reports to", "rival of", "member of") rather than free-text cross-references. Notes and relationships can be marked as a GM-only secret, kept distinct from ordinary prep content so a later player-facing view can omit them without deleting or hiding them from the game master.

## Acceptance criteria

- Given a configured campaign, when the people-and-factions catalog is opened, then campaign-owned people, factions, and NPCs (Story 18) are searchable together by name and filterable by tag, role, or status.
- Given a new person or faction, then the game master can create it directly in the campaign without requiring a source rules record, distinct from NPC creation which may originate from a package record.
- Given two catalog entries, then the game master can create, label, and remove a typed relationship between them; relationships are directional or symmetric as their type requires.
- Given a catalog entry, then its relationships are visible from either endpoint and remain resolvable if the other entry's name or tags change.
- Given a note or relationship marked secret, then it is stored distinctly from ordinary content and always visible to the game master, without implying any player-facing access in this release.
- Given a removed catalog entry, then relationships referencing it are identified rather than silently discarded or left dangling.
- Given many catalog entries, then search and filtering remain fast and do not require loading full relationship graphs eagerly.
- Given keyboard or assistive-technology use, then catalog search, filters, entry editing, and relationship management are operable and meaningfully labelled.

## Not included

Player-facing views or access control, faction-level automated behavior or standing/reputation mechanics, org-chart or graph visualization, bulk import, procedural relationship generation, or encounter/initiative integration.
