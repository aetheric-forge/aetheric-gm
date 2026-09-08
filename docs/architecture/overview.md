# Architecture overview

## GM institution and standalone Campus

GM is developed and operated on its own Campus. `AethericGm.Web` owns that standalone host;
`AethericGm.Institutions.Gm` contributes a reusable child institution. Both the standalone
host and a future Forge host register it with `campus.RegisterAethericGm(services)`.

`IAethericGm` extends runtime `IInstitution`, `AethericGmInstitution` inherits runtime
`InstitutionBase`, and its context requires a parent institution. Registration, parent
scope resolution, and lifecycle contracts come from runtime. GM does not implement a
second institution registry, Campus, Library, or identity framework.

The standalone host currently registers the runtime Registry and GM under one root Campus.
Archive, Library, Post Office, and Workbench are not yet provisioned by this host; do not
assume their presence or describe the local repositories as implementations of them.
When a game feature needs one of these capabilities, compose the actual runtime institution
and provider in the host, then use its contracts. The GM institution can already resolve
capabilities from its parent through runtime's normal scope lookup.

## Project boundaries

- `AethericGm.Core`: game entities, invariants, and persistence ports; no hosting dependencies.
- `AethericGm.Institutions.Gm`: runtime institution exposing GM capabilities; references Core
  and runtime, never Web, SQLite, configuration, or authentication middleware.
- `AethericGm.Infrastructure`: SQLite/file/Git adapters, rules workspace resolution, and
  optional `AddLocalGmStorage` registration with explicit absolute paths.
- `AethericGm.Web`: standalone Campus composition, provider configuration, authentication,
  credential protection, routing, and Blazor UI. Pages consume the registered `IAethericGm`.
- `AethericGm.Tests`: game behavior, persistence, and host portability tests.
- `runtime`: pinned upstream dependency; not modified here.

## Identity

The standalone web host authenticates with Forge Keycloak using OpenID Connect with PKCE,
validates tokens, and maintains a secure authentication cookie. Secrets remain external
configuration. A runtime Registry is registered beneath the standalone Campus.

The UI currently obtains the authenticated subject ID from claims; the host's
`ResolveOperatorAsync` helper additionally supports runtime Registrar resolution. The
portable institution does not select an issuer, create login endpoints, or own cookies.
A future integrated host supplies its existing authenticated identity at the UI boundary.

Profiles and SSH credential metadata use that subject ID. They are application integration
data, not a replacement identity registry. See [User profiles and SSH credentials](user-profiles-and-ssh.md).
Campaigns and selected workspace state remain single-operator; portable hosting does not
introduce campaign sharing or per-user campaign authorization.

## Persistence and lifecycle

`AddLocalGmStorage` keeps the existing SQLite schema and rules-package format. Its host
supplies absolute data/rules paths and `ISshPrivateKeyProtector`, then calls
`InitializeLocalGmStorageAsync` before accepting requests. Startup performs idempotent
schema initialization. Existing data paths and protection settings are unchanged in the
standalone host.

The host owns institution lifecycle order. It initializes and starts the parent and
Registry before GM, then stops GM before its parent. Registration alone does not start
institutions; runtime's current base lifecycle methods do not cascade to children.

See [Hosting and migration](hosting-and-migration.md) for the integration contract and remaining host work.
