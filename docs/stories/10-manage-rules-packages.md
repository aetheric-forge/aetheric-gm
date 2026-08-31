# Manage installed rules packages

As an operator, I want to inspect package provenance and manage sources separately from installed content so that campaigns remain understandable when repositories or permissions change.

## Outcome

The application shows installed package identity, source, pinned revision, license metadata, validation status, and campaign usage. Source connections and installed packages have separate lifecycles.

## Acceptance criteria

- Given an installed package, then the operator can view its ruleset identity, source kind, sanitized source location, commit when applicable, installation time, license metadata, and validation status.
- Given a removed or inaccessible source, then its installed packages remain available from the managed cache.
- Given removal of a source connection, then no installed package is removed implicitly.
- Given an installed package referenced by campaigns, when removal is requested, then the application identifies those campaigns and requires an explicit decision.
- Given a removed installed package, then campaign records retain their unavailable ruleset references and remain readable.
- Given duplicate ruleset ID and version claims, then the catalog exposes at most one explicitly selected installation and reports the conflict.

## Not included

License adjudication, transferring repository access, deleting campaign data, or automatically replacing unavailable packages.
