# Architecture overview

## Standalone Campus boundary

Aetheric GM is currently one independently hosted Campus: its composition root is the constitutional root and has no parent. The application references checked-in runtime projects directly; the runtime submodule remains unmodified. Required Archive, Library, Post Office, and Registrar capabilities belong beneath that root and must be registered through institutional scope, never treated as ambient application services.

The campaign slice does not claim those runtime institutions as its persistence mechanism. Campaign records are application-domain data accessed through `ICampaignRepository`; SQLite is an adapter behind that interface.

## Project boundaries

- `AethericGm.Core`: entities, invariants, and persistence ports.
- `AethericGm.Infrastructure`: local SQLite adapters and schema initialization.
- `AethericGm.Web`: composition root and Blazor UI.
- `AethericGm.Tests`: domain and adapter behavior.
- `runtime`: pinned upstream submodule; project-referenced and not edited here.

Dependencies point inward: Web may use Core, Infrastructure, and runtime; Infrastructure uses Core; Core uses neither UI nor database libraries.

## Identity

The application authenticates through the existing Forge Keycloak realm using OpenID Connect authorization code flow with PKCE. ASP.NET Core validates tokens and maintains an HTTP-only, secure authentication cookie; secrets remain external configuration. The authenticated `sub` claim is resolved through the runtime Keycloak provider, `IdentityService`, and the Campus-owned Registry Registrar.

The Registry is a direct child of the standalone Campus. Authentication establishes the operator identity but does not yet introduce application roles or campaign-sharing authorization. All campaign data remains single-operator in this slice.

Aetheric GM maintains a local application profile keyed by the authenticated `sub` claim. This profile is not an authentication account; it owns application-specific integration metadata, initially SSH credentials for private rules-package acquisition. Credential handling and storage are defined in [User profiles and SSH credentials](user-profiles-and-ssh.md).

## Future Forge Campus integration seam

Future integration supplies a parent host and mounts Aetheric GM as an explicitly constituted descendant institution or application capability. Campaign identifiers and repository contracts remain unchanged. Integration replaces host composition and infrastructure registration—not domain entities or Razor-owned business rules. Until then, Aetheric GM remains exactly one root Campus and does not simulate federation.

## Persistence

SQLite lives under the host's `App_Data` directory. Startup performs idempotent schema creation. Campaign and selected-workspace state share the database, so reopening restores context. Later schema evolution should use ordered migrations.
