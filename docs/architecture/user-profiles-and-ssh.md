# User profiles and SSH credentials

Authentication establishes an external identity; Aetheric GM owns a small local user profile keyed by the authenticated subject identifier. The profile contains application preferences and credential metadata needed for user-initiated integrations. In v0.1 its only editable capability is private SSH credential management.

The user profile is not an authentication account and does not replace Keycloak. A profile cannot sign in, grant application roles, or be selected by another user. Campaign and package operations always resolve the current profile from the authenticated subject.

## Credential model

An SSH credential contains:

- an opaque application-generated ID;
- an owner profile ID;
- a user-provided display name;
- encrypted private-key material;
- key algorithm and public-key fingerprint derived by the application;
- creation and last-used timestamps.

The private key is accepted only through the credential-management surface. After creation, the application returns metadata but never returns or displays the private-key material. Application logs, validation messages, command arguments, provenance records, campaign exports, and rules-package records must never contain it.

Credential names are not identities and need only be unique within one profile. Package sources refer to a credential by its opaque ID. Removing or replacing a credential does not rewrite Git provenance or installed package content.

## Storage protection

Private-key material is encrypted before persistence with application data-protection keys stored separately from the profile database. Database access alone must not reveal a usable key. Data-protection keys and encrypted credential records require operating-system permissions limited to the application identity, and production deployment must provide durable protection keys rather than ephemeral startup keys.

A decrypted key exists only for the duration of an authorized Git operation. If the Git client requires a file, the application creates a uniquely named private temporary file with owner-only permissions, passes its path without shell interpolation, and removes it in a `finally` path. The key must not be placed in process arguments, environment-variable values, standard output, or standard error.

Passphrase-protected keys remain encrypted in storage. Their passphrase is requested when the credential is used, retained only in memory for that operation, and never persisted. An SSH agent may be supported as a credential that holds no private-key material in Aetheric GM.

## SSH host identity

Private-key authentication proves the client to the Git host; it does not prove the host to the client. Aetheric GM must verify SSH host keys and must never disable strict host-key checking.

For a previously unknown host, the application presents the hostname, algorithm, and fingerprint for explicit user acceptance before transmitting a credential. Accepted host keys are stored separately from private credentials in an application-managed known-hosts store. A changed host key blocks acquisition until the user explicitly reviews it; it is never accepted automatically.

## Authorization and lifecycle

Only the owning authenticated profile may list, select, use, rename, or remove a credential. List and detail views expose metadata only. A credential selected by a package source may be removed, but the application first explains which configured sources will lose future private access. Already installed packages remain available from the managed cache.

The current product is single-operator, but ownership is modeled explicitly so later multi-user hosting does not expose credentials or privately acquired packages across profiles.

## Trust boundary

SSH keys authorize access beyond Aetheric GM and are therefore higher-sensitivity data than campaign content. Credential parsing, fingerprint derivation, decryption, temporary materialization, Git invocation, host verification, and cleanup belong in Infrastructure behind Core ports. Razor components never handle persisted ciphertext or decrypted key objects directly.
