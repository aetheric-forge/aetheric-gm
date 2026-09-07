# Install a rules package from a local directory

As a rules author or authorized user, I want to install a declarative rules package from a local directory so that I can validate and use content obtained or developed outside Aetheric GM.

## Outcome

The application stages, validates, and atomically copies a selected local package into its managed cache. The Rules Catalog discovers only the installed copy; subsequent edits to the source directory do not silently alter it.

## Acceptance criteria

- Given a selected directory, when installation begins, then the source is staged without executing scripts or code.
- Given a valid package, when validation succeeds, then its immutable version is installed atomically and appears in the Rules Catalog.
- Given an invalid or unsupported package, when validation fails, then no partial installation is visible and the diagnostic identifies the relevant package document.
- Given symbolic links, path traversal, excessive files, excessive document depth, or configured size limits, then installation fails safely.
- Given an installed package, when its source directory changes or disappears, then the installed package remains unchanged and available.
- Given provenance, then the installation records its local source kind and location without copying credentials or unrelated files.

## Not included

Watching a source directory, automatically reinstalling changes, executing package build steps, or migrating campaign data.
