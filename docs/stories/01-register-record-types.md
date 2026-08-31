# Register record types

As a rules author, I want to define reusable record types within a versioned ruleset so that game concepts can have structured, composable shapes without requiring application code.

## Outcome

A ruleset package can register immutable record-type definitions. The catalog resolves their inheritance and exposes the valid types to authoring tools.

The first Shadowdark definitions should be sufficient to express:

- an `attribute` with a value, optional bounds, optional bonus, and repeated temporary modifiers;
- a `modifier` with a source, amount, optional duration, and optional note;
- a named `ability` with descriptive text;
- a named `ancestry` with flavour text and repeated ability references.

## Acceptance criteria

- Given a valid record-type document, when the ruleset is loaded, then every type is available by its stable key in that ruleset version.
- Given a type with a parent, when it is resolved, then its effective fields contain the parent's ordered fields followed by its own fields.
- Given a field, when it is registered, then its value kind and cardinality are validated independently.
- Given a record or reference field, when it is registered, then its target record type must exist.
- Given duplicate keys, incompatible inherited fields, an unknown parent or target, or an inheritance cycle, when the package is loaded, then loading fails with a useful diagnostic.
- Given a reference to a parent record type, when compatibility is checked, then records of descendant types are accepted.
- Given two versions of a ruleset, then each version has an independent registry and neither can inherit types from the other.

## Not included

Runtime generation of CLR subclasses, cross-ruleset inheritance, calculations, executable validation expressions, or persisted character values.
