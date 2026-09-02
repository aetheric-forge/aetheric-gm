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
| P1 | Hierarchical rules catalog | Organize declarative rules records into authoring and consumption sections without duplicating their content. |
| P1 | Local rules catalog editing | Edit schema-driven catalog entries and published records as validated JSON in the installed local package copy. |
| P1 | Character creation skeleton | Discover character-creation catalogs and retain stable references to selected rules records such as ancestries. |
| P1 | Animated dice roller | Roll recognizable 2D polyhedral dice with accessible animation, contextual modifiers, readable breakdowns, and recent history. |
| P1 | Inline rules rolls | Render safe declarative dice links in rules prose and resolve them in a compact modal backed by shared session history. |
| P1 | Character sheet proper | Use a focused ruleset character sheet whose meaningful values and actions can initiate contextual rolls. |
| P1 | NPC catalog (v0.3) | Find, inspect, create, and use campaign NPCs through an explicitly designed NPC state and action vocabulary. |
| P1 | User profile and SSH credentials | Securely manage profile-owned private SSH credentials and verified Git hosts. |
| P1 | External rules packages | From a campaign, validate and install separately licensed packages from pinned public/private SSH Git revisions. |
| P1 | Campaign rules selection | Select an installed ruleset and reopen the workspace from the local cache. |
| P1 | Package lifecycle | Inspect provenance, explicitly update packages, and retain stable offline installations when sources disappear. |
| P1 | Session workspace | Draft/running/completed sessions with prep, live notes, and recap. |
| P1 | People and factions | Campaign-scoped records, tags, relationships, distinct secrets, fast retrieval. |
| P1 | Places | Nested locations plus explicit non-containment relationships. |
| P1 | Encounters | Reusable templates and independent prepared/running/resolved session instances. |
| P1 | Initiative and live play | Stable reorderable ties, rounds, and refresh-safe temporary state. |
| P2 | Quick reference | Pinned notes and fast access to frequently used rules. |
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

Version 0.1 established private rules package loading and a rules-enabled campaign workspace. The current target is the [v0.2 live-play foundations milestone](v0.2.md): animated 2D dice, inline rolls from rules prose, character creation, and a proper character sheet. Version 0.2 ends with Story 17; the NPC catalog begins the [v0.3 campaign-world milestone](v0.3.md).
