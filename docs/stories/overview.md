# Rules authoring stories

These stories evolve the character-sheet designer from a collection of scalar controls into a rules-neutral composition system. They deliberately separate reusable schemas, published rules content, and mutable character state.

The stories should be delivered in order:

1. [Register record types](01-register-record-types.md)
2. [Publish rules records](02-publish-rules-records.md)
3. [Compose character sheets from records](03-compose-character-sheets.md)

Shadowdark provides the first proving examples: attributes contain structured values and temporary modifiers, while ancestries are published rules records that can carry flavour text and grant abilities.

Calculations, automatic effects, creation procedures, and persisted character values are intentionally deferred. The purpose of this sequence is to establish the vocabulary those later capabilities will use.
