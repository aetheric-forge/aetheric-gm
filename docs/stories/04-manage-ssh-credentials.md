# Manage SSH credentials in the user profile

As an authenticated user, I want to manage private SSH credentials in my Aetheric GM profile so that I can explicitly authorize access to private rules-package repositories.

## Outcome

The user profile provides its first editable capability: add, list, inspect, rename, and remove private SSH credentials. Private material is encrypted at rest and is never shown again after submission.

## Acceptance criteria

- Given an authenticated user, when the profile is opened, then only credentials owned by that profile are listed.
- Given a supported private key, when it is added with a display name, then the application validates it, derives its algorithm and public-key fingerprint, encrypts it, and persists only encrypted material plus metadata.
- Given a stored credential, then list and detail views show its name, algorithm, fingerprint, creation time, and last-used time without exposing private material.
- Given application logs, errors, process arguments, exports, or provenance, then private-key material and passphrases never appear.
- Given a passphrase-protected key, then its passphrase is requested when used and is never persisted.
- Given a rename, then the opaque credential identity and encrypted key remain unchanged.
- Given removal, then configured Git sources that reference the credential are identified before confirmation, while already installed packages remain available.
- Given encrypted records copied without the application's protected data keys, then they cannot be used as SSH private keys.
- Given temporary key material required by Git, then it has owner-only permissions and is removed after success, failure, or cancellation.

## Not included

Application passwords, API tokens, HTTPS Git credentials, credential sharing between users, SSH certificate authorities, or general-purpose secret storage.
