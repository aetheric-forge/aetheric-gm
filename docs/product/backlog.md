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
| P1 | User profile and SSH credentials | Securely manage profile-owned private SSH credentials and verified Git hosts. |
| P1 | External rules packages | From a campaign, validate and install separately licensed packages from pinned public/private SSH Git revisions. |
| P1 | Campaign rules selection | Select an installed ruleset and reopen the workspace from the local cache. |
| P1 | Package lifecycle | Inspect provenance, explicitly update packages, and retain stable offline installations when sources disappear. |
| P1 | Session workspace | Draft/running/completed sessions with prep, live notes, and recap. |
| P1 | People and factions | Campaign-scoped records, tags, relationships, distinct secrets, fast retrieval. |
| P1 | Places | Nested locations plus explicit non-containment relationships. |
| P1 | Encounters | Reusable templates and independent prepared/running/resolved session instances. |
| P1 | Initiative and live play | Stable reorderable ties, rounds, and refresh-safe temporary state. |
| P2 | Dice and quick reference | Rules-neutral expressions, readable breakdowns, pinned notes. |
| P2 | Search and linking | Campaign-bounded typed results and resilient links. |
| P2 | Portability and trust | Validated atomic import/export that excludes licensed rules packages by default and never exports source credentials. |

## Assumptions

- One local operator and local SQLite storage.
- Campaign is the ownership boundary for game material.
- Domain rules remain independent of Razor and database technology.
- Runtime institutional containment is structural; navigation does not redefine it.
- Timestamps are stored in UTC and displayed locally.
- Licensed third-party rules content is installed separately and is not distributed with Aetheric GM without appropriate permission.
- Private integration credentials belong to the authenticated user's local application profile and are encrypted separately from campaign data.

## Out of scope for the MVP

Authentication/authorization UI, generative AI, bundled third-party rules compendia without redistribution permission, multiplayer/player views, cloud sync, and Forge Campus integration.

## Current slice

Phase 0 plus campaign workspace and rules authoring are complete foundations. The next target is the [v0.1 private rules package milestone](v0.1.md): profile-owned SSH credentials, campaign-initiated Git package loading, ruleset selection, and offline workspace reopening.
