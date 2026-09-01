# Rules authoring stories

These stories evolve the character-sheet designer from a collection of scalar controls into a rules-neutral composition system, then allow separately licensed packages to be installed without distributing their content with Aetheric GM. They deliberately separate reusable schemas, published rules content, mutable character state, and package acquisition.

The v0.1 stories should be delivered in order:

1. [Register record types](01-register-record-types.md)
2. [Publish rules records](02-publish-rules-records.md)
3. [Compose character sheets from records](03-compose-character-sheets.md)
4. [Manage SSH credentials in the user profile](04-manage-ssh-credentials.md)
5. [Load a rules package from Git for a campaign](05-load-git-rules-package.md)
6. [Select a ruleset for a campaign](06-select-campaign-ruleset.md)
7. [Open a configured campaign workspace](07-open-configured-campaign-workspace.md)

Together these form the [v0.1 private rules package milestone](../product/v0.1.md).

Later package-lifecycle stories are:

8. [Install a rules package from a local directory](08-install-local-rules-package.md)
9. [Update an installed rules package](09-update-rules-package.md)
10. [Manage installed rules packages](10-manage-rules-packages.md)
11. [Protect licensed rules during export](11-protect-rules-during-export.md)

The next rules-authoring and character-creation stories are:

12. [Organize rules content in a catalog](12-organize-rules-catalog.md)
13. [Edit a local rules catalog](13-edit-rules-catalog.md)
14. [Save local rules edits safely](14-save-local-rules-edits-safely.md)
15. [Choose an ancestry during character creation](15-choose-character-ancestry.md)

The v0.2 live-play stories are:

16. [Roll animated dice](16-roll-animated-dice.md)
17. [Use a character sheet during play](17-use-character-sheet.md)
18. [Manage an NPC catalog](18-manage-npc-catalog.md)
19. [Roll dice from rules text](19-roll-from-rules-text.md)

Together these form the [v0.2 live-play foundations milestone](../product/v0.2.md). Inline rules rolls are numbered after the original milestone stories but are intended as a companion to the dice tray before character-sheet and NPC work begins.

An original demonstration package provides the distributable proving examples: attributes contain structured values and temporary modifiers, while heritages are published rules records that can carry flavour text and grant abilities. A separately obtained Shadowdark package may use the same engine, but Shadowdark content is not part of the application repository or release artifacts without an appropriate redistribution license.

Calculations, automatic effects, creation procedures, and persisted character values were intentionally deferred from the rules-authoring sequence. The v0.2 stories can now build on that vocabulary without coupling game state or dice behavior to one rules package.
