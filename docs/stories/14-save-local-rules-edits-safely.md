# Save local rules edits safely

As a rules author, I want local package edits to be validated and saved atomically so that a failed edit cannot corrupt the ruleset used by my campaigns.

## Outcome

Rules authoring writes a complete candidate document to an isolated temporary location, validates the package as a whole, and replaces the affected local JSON document only when validation succeeds. The installed working copy may diverge from its pinned upstream revision, and that state is made explicit.

## Acceptance criteria

- Given a catalog or records edit, when saving begins, then the candidate JSON is written without partially replacing the active document.
- Given a valid candidate, then the complete local package is revalidated before the affected document is atomically activated.
- Given invalid JSON, schema violations, unresolved references, excessive document depth or size, or an interrupted write, then the previous valid local package remains available.
- Given removal of a record referenced by another rules record, then saving is blocked and the diagnostic identifies the dependency.
- Given a successful local edit, then the package is visibly marked as locally modified relative to its recorded installed commit or source revision.
- Given a locally modified package and a requested upstream update, then the application warns that activation may replace local edits and requires an explicit decision.
- Given successful activation, then campaigns using the same installed ruleset reference see the updated local catalog without changing their ruleset ID or version.
- Given saved rules content, then only supported declarative package files are changed and no executable content is introduced or run.

## Not included

Revision history, three-way merge, rollback beyond preserving the last valid document during a failed save, automatic version changes, or upstream publication.
