# Select a ruleset for a campaign

As a game master, I want to select one ruleset from the packages available to my campaign so that its workspace opens with a stable, explicit rules context.

## Outcome

The existing Campaign editor lists rulesets from successfully installed user-accessible packages and persists the selected `RulesetReference`. Loading a package and selecting a ruleset are separate actions, so one package may expose more than one ruleset or version.

## Acceptance criteria

- Given an installed and validated package, when the Campaign editor is opened, then its available rulesets are listed by name and version.
- Given multiple installed packages, then duplicate ruleset ID and version conflicts are identified rather than silently selecting one.
- Given a ruleset selection, when the campaign is saved, then its stable ruleset ID and version are persisted.
- Given a reopened campaign, then its selection is restored without contacting Git.
- Given a referenced ruleset whose installed package is unavailable, then the reference is retained, the campaign remains readable, and the editor identifies it as unavailable.
- Given a package containing multiple rulesets or versions, then selecting one does not implicitly update or select another.
- Given a changed selection, then no character or campaign data migration is inferred.

## Not included

Ruleset migration, automatic updates, character creation, or package removal.
