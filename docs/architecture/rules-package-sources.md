# Rules package sources

Aetheric GM must support rules content that its own maintainers and distributors are not licensed to redistribute. The application repository and release artifacts therefore contain the rules engine and, at most, clearly original demonstration packages. Licensed third-party rules packages are acquired separately by an authorized user.

The application does not assume that a public repository grants redistribution rights. Public and private Git repositories are both transport mechanisms; the package license remains authoritative.

## Boundaries

Package acquisition, installation, and catalog discovery are separate responsibilities:

```text
Rules package source
  -> acquire an explicit revision
  -> copy into a managed staging area
  -> validate the complete package
  -> atomically install into the local package cache
  -> expose the installed version through the Rules Catalog
```

`IRulesPackageSource` represents acquisition. Source configuration and installed content have separate lifecycles. Initial adapters are:

- a local directory for authoring and deliberately selected packages;
- a Git repository for public or private distribution;
- a managed or bundled source reserved for content Aetheric GM may redistribute.

The Rules Catalog reads only successfully installed packages from the managed local cache. It does not clone repositories, prompt for credentials, follow branches, or mutate source working trees.

## Git acquisition

A Git installation records the repository URL and an exact commit hash. A branch or tag may help the user select a revision, but the installed package is always pinned to the resolved commit. The application never silently advances an installed package when its branch changes.

The v0.1 application accepts SSH Git repository URLs. A public repository may require no client credential; a private repository selects an SSH credential owned by the authenticated user's local profile. Credential storage, temporary materialization, and SSH host verification follow [User profiles and SSH credentials](user-profiles-and-ssh.md).

The acquisition adapter invokes Git without a command shell, supplies the selected credential through an isolated SSH configuration, and sanitizes repository URLs before persistence or display. HTTPS credentials and tokens are outside v0.1. Agent-backed SSH may be added without changing the package-source contract.

Acquisition must not execute hooks, package scripts, build steps, or code from the repository. Only the declarative package files required by the supported format are copied into staging and interpreted.

## Validation and installation

A candidate revision is fully validated before it becomes visible to campaigns. Validation includes its manifest, record-type registry, rules records, character-sheet definitions, internal references, supported format version, and package identity. Installation fails without disturbing the currently installed package when validation fails.

Installed content is copied into an application-managed cache rather than read continuously from the source checkout. This provides stable offline behavior, prevents later source edits from changing a running campaign, and allows a private repository to become temporarily unavailable without invalidating an authorized local installation.

Installation is initiated from the Campaign editor in v0.1, but the validated package is stored once in the owning user's managed cache. A campaign does not own or duplicate package files. It stores only its selected `RulesetReference`; installed-package provenance maintains the association with its source and pinned commit.

An installed package records provenance separately from its rules content:

- source kind;
- source location, with secrets removed;
- resolved Git commit when applicable;
- installation timestamp;
- package identity and version;
- validation status and supported package-format version.

Credentials are referenced by opaque profile-owned IDs and are not part of package provenance. Sanitized provenance must remain safe to display, log, and export.

Published package identity remains ruleset ID plus ruleset version. A different commit claiming the same published identity must be treated as a replacement candidate requiring an explicit update, never as an invisible mutation.

## Updates and removal

Checking for updates is read-only. Installing an update is explicit, resolves a new commit, validates it in isolation, and atomically activates it. Existing character and campaign references do not migrate automatically between ruleset versions.

Removing a source connection does not remove its installed packages. Removing an installed package is a separate action and must warn when campaigns reference it. Campaigns retain unavailable `RulesetReference` values so their own data remains readable.

## Licensing and export

The manifest may declare a license name, license URL, and a redistribution disposition such as `allowed`, `prohibited`, or `unspecified`. Aetheric GM uses this metadata to explain provenance and choose safe export defaults. It does not infer permission merely because a repository is public and does not claim to enforce the underlying license.

Campaign export excludes installed rules packages by default. A package marked `prohibited` must not be embedded by ordinary export flows. A package with `unspecified` redistribution requires an explicit warning and separate user choice before any future package-inclusive export. Package provenance may be exported without copying the rules content so another authorized installation can attempt to resolve the same source and revision.

## Trust model

Rules packages are untrusted data. The application imposes file-size, document-depth, record-count, and reference-resolution limits; rejects path traversal and symbolic-link escapes during acquisition; and never interprets package content as executable code. These controls apply equally to local, public Git, private Git, and bundled sources.
