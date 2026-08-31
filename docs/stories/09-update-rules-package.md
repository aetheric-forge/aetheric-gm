# Update an installed rules package

As an authorized user, I want to inspect and explicitly install a newer source revision so that package updates do not silently alter campaigns.

## Outcome

The application can check a configured source, show the current and candidate revisions, validate the candidate independently, and atomically activate an approved update.

## Acceptance criteria

- Given a configured source, when the user checks for updates, then the operation does not modify installed content.
- Given a candidate branch or tag, then it is resolved to an exact commit before installation.
- Given a candidate claiming the same ruleset ID and version with different content, then it is presented as an explicit replacement rather than silently accepted.
- Given a valid candidate, when the user installs it, then activation is atomic and provenance records the new commit.
- Given an invalid candidate or acquisition failure, then the existing installed version remains active.
- Given a new ruleset version, then existing campaigns retain their current version references and are not migrated automatically.

## Not included

Automatic campaign migration, unattended updates, semantic comparison of game rules, or rollback history beyond retaining the previously active installation during validation.
