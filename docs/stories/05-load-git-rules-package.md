# Load a rules package from Git for a campaign

As an authenticated user configuring a campaign, I want to load a rules package from a public or private SSH Git repository so that licensed content can be obtained independently of Aetheric GM.

## Outcome

From the Campaign editor, the user supplies an SSH repository URL and selects a profile-owned SSH credential when private access requires one. The application acquires an explicitly selected revision, validates its declarative package, and installs one immutable user-scoped copy pinned to the resolved commit.

## Acceptance criteria

- Given a syntactically valid SSH repository URL, when it is entered, then it is normalized and stored without embedded secrets.
- Given a private repository, when loading begins, then the user selects an SSH credential owned by their profile.
- Given a public repository, then loading may proceed without a client credential when the host permits it.
- Given a repository URL and selected branch, tag, or commit, when acquisition succeeds, then the installation records the exact resolved commit hash.
- Given an unknown SSH host, before any credential is transmitted, then the user must review and accept its host-key fingerprint.
- Given a changed host key, then loading is blocked until the change is explicitly reviewed; strict host checking is never disabled.
- Given a selected credential, then its private material is decrypted only for the operation and is absent from Git arguments, logs, errors, and provenance.
- Given a repository, when it is acquired, then hooks, scripts, builds, and package code are not executed.
- Given a valid package at the selected revision, then only supported package content is staged, validated, and atomically installed.
- Given an acquisition or validation failure, then any existing installation remains active and staging data can be safely discarded.
- Given a moving branch, then the installed package does not change until the user explicitly requests an update.
- Given a public repository, then the application does not represent public accessibility as permission to redistribute its contents.
- Given a successfully installed package, then the Campaign editor can immediately present its rulesets without duplicating package files into the campaign.

## Not included

Hosting Git repositories, HTTPS credentials, automatic background updates, executing repository tooling, or selecting the campaign's final ruleset.
