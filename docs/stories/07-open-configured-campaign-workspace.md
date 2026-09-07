# Open a configured campaign workspace

As a game master, I want a configured campaign to open using its locally installed ruleset so that private Git access is not required during ordinary play.

## Outcome

The existing campaign workspace becomes the v0.1 composition boundary. Creating a campaign, loading a package, and selecting a ruleset lead into that workspace; no additional persistent Workspace aggregate is introduced.

## Acceptance criteria

- Given a campaign with an available selected ruleset, when it is opened, then the workspace resolves that ruleset from the managed local package cache.
- Given a private source that is offline or no longer authorized, then an already installed package remains usable.
- Given a campaign without a selected ruleset, then the rules-neutral workspace remains available and offers a path back to campaign configuration.
- Given an unavailable selected ruleset, then campaign-owned data remains readable and the workspace offers package recovery without discarding the reference.
- Given two campaigns selecting the same installed package version, then they share the immutable cached installation without sharing campaign-owned state.
- Given a package load or validation failure during setup, then the campaign remains valid and can still open rules-neutral.

## Not included

A separate Workspace entity, multiplayer workspace sharing, live package updates, or campaign-data migration.
