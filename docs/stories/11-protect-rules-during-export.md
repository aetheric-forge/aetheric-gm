# Protect licensed rules during export

As an operator, I want campaign export to keep licensed rules packages separate by default so that sharing my campaign does not inadvertently redistribute third-party content.

## Outcome

Campaign export contains campaign-owned data and stable package provenance without copying installed rules content unless a future explicitly authorized package-inclusive export permits it.

## Acceptance criteria

- Given any campaign export, then installed rules packages are excluded by default.
- Given an excluded package, then the export may include sanitized provenance sufficient for another authorized installation to identify its source and pinned revision.
- Given a package whose redistribution is `prohibited`, then ordinary export flows cannot embed its rules content.
- Given a package whose redistribution is `unspecified`, then any future package-inclusive export requires a clear warning and explicit user choice.
- Given a package whose redistribution is `allowed`, then inclusion remains an explicit export option rather than the default.
- Given source provenance, then credentials, access tokens, private keys, and credential-bearing URLs are never exported.
- Given an imported campaign without its package, then the campaign remains readable and its ruleset reference is shown as unavailable until an authorized package is installed.

## Not included

Determining whether a license declaration is legally correct, granting repository access, or circumventing package protections.
