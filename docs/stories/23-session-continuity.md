# Story 23 — continue the campaign's story

As a game master, I want to begin the next session from selected notes and unresolved threads,
so that I can prepare the next chapter without duplicating or rewriting the previous record.

Acceptance criteria:

1. Create next session is available from a notebook; unsaved source edits must save before navigating.
2. The GM can select Markdown text, copy all and edit, or start blank. Nothing is copied implicitly.
3. Creation produces a new Draft in the same campaign with independent identity and revision history.
4. Both sessions provide stable navigation links; the child retains the exact source revision.
5. Later source edits/renames do not change copied notes or source-revision provenance.
6. Source relationships cannot be redirected after creation, or point into another campaign.
7. Existing notebooks and their revisions survive the additive schema upgrade.
8. The creation form retains entered notes after validation errors and warns before discarding them.

See the v0.4 milestone document for the manual QA checkpoint. Browser integration requires
local QA; automated checks cover model/storage behavior and JavaScript text selection.
