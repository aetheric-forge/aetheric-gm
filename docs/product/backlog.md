# Aetheric GM product backlog

Aetheric GM is a rules-neutral, single-GM campaign helper. Priority is a trustworthy local workspace first; live-play speed and portability follow.

## Priorities

| Priority | Slice | Outcome |
|---|---|---|
| P0 | Campus foundation | Standalone Campus boundary, runtime project references, documented integration seam, buildable app. |
| P0 | Campaign workspace | Create, rename, open, edit, archive, restore, and retain selection. |
| P1 | Rules record types | Register reusable, inherited record shapes with typed fields and cardinality. |
| P1 | Published rules records | Load immutable rules content such as ancestries and abilities with stable references. |
| P1 | Composable character sheets | Embed character-owned records and reference published rules records from sheet fields. |
| P1 | Session workspace | Draft/running/completed sessions with prep, live notes, and recap. |
| P1 | People and factions | Campaign-scoped records, tags, relationships, distinct secrets, fast retrieval. |
| P1 | Places | Nested locations plus explicit non-containment relationships. |
| P1 | Encounters | Reusable templates and independent prepared/running/resolved session instances. |
| P1 | Initiative and live play | Stable reorderable ties, rounds, and refresh-safe temporary state. |
| P2 | Dice and quick reference | Rules-neutral expressions, readable breakdowns, pinned notes. |
| P2 | Search and linking | Campaign-bounded typed results and resilient links. |
| P2 | Portability and trust | Validated atomic import/export with explicit secret export. |

## Assumptions

- One local operator and local SQLite storage.
- Campaign is the ownership boundary for game material.
- Domain rules remain independent of Razor and database technology.
- Runtime institutional containment is structural; navigation does not redefine it.
- Timestamps are stored in UTC and displayed locally.

## Out of scope for the MVP

Authentication/authorization UI, generative AI, third-party rules compendia, multiplayer/player views, cloud sync, and Forge Campus integration.

## Current slice

Phase 0 plus campaign workspace, followed by the rules-authoring sequence described in [`docs/stories`](../stories/overview.md). Archived campaigns are hidden by default, selection is persisted, and optional system, setting, and summary metadata can be edited.
