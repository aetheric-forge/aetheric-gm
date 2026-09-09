# Story 22 — session notebooks

As a game master, I want a continuous notebook for each session so that preparation and live notes
can become a polished story without losing the original record.

Acceptance criteria:

1. Create and find notebooks within their campaign, without requiring a selected rules package.
2. Edit a title, optional date, lifecycle state, and one freeform Markdown document.
3. Preview the narrative and link to campaign-owned entities through stable IDs.
4. Show autosave progress and errors; recover pending tab-local drafts after refresh where browser storage is available.
5. Preserve each successful save as an immutable revision and restore any revision non-destructively.
6. Reject stale saves and prevent campaign IDs from being reassigned through repository operations.
7. Keep the notebook usable without Conversation or TableTop providers, and keep provider-specific payloads out of persistence/UI.

A session can be reopened by changing its status. Completed does not make its narrative immutable;
revision history preserves the working record while the GM edits it.
