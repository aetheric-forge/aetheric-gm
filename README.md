# Aetheric GM

A rules-neutral campaign helper for a single game master. Local-first, offline-capable, and built so that licensed third-party rulesets never have to live in this repository.

> **Status:** early and actively evolving. v0.1 shipped private rules-package loading and a rules-enabled campaign workspace; v0.2 (in progress) adds dice rolling, character creation, and a proper character sheet. See the [product backlog](docs/product/backlog.md) for the full picture.

## What it does

- **Rules-neutral by design.** Game systems are declarative data — record types, published rules records, and character-sheet definitions — not compiled C# tied to one ruleset. A new system can be added without changing the application.
- **Licensed content stays licensed.** Aetheric GM ships only the rules engine and an original demonstration package. Real rulesets (e.g. Shadowdark) are installed separately by an authorized user from a pinned Git commit, over SSH, and are excluded from campaign export by default.
- **A local workspace you can trust offline.** Once a rules package is installed and validated, campaigns reopen from a local cache — no network access, no re-cloning, no silently advancing to a newer commit.
- **Built for the table.** Animated 2D dice, inline `[roll:...]` links inside rules prose, and a character sheet that stays in sync with the ruleset that governs it.

## Architecture at a glance

Aetheric GM is a standalone [Forge](docs/architecture/overview.md) Campus — its composition root has no parent (yet). It's a layered .NET solution:

| Project | Responsibility |
|---|---|
| `AethericGm.Core` | Entities, invariants, persistence ports. No UI or database dependencies. |
| `AethericGm.Infrastructure` | SQLite adapters, schema initialization, credential/Git handling. |
| `AethericGm.Web` | Composition root and Blazor UI. |
| `AethericGm.Tests` | Domain and adapter behavior. |
| `runtime` | Pinned upstream Forge runtime submodule (Campus, identity, registry). Not edited here. |

Authentication runs through Forge's Keycloak realm via OpenID Connect. See [`docs/architecture/`](docs/architecture) for the full design, including how private SSH credentials and third-party rules packages are handled safely.

## Getting started

Aetheric GM depends on the private `aetheric-runtime` submodule, so building it requires access to the `aetheric-forge` organization.

```sh
git clone --recurse-submodules git@github-aetheric:aetheric-forge/aetheric-gm
cd aetheric-gm
dotnet build AethericGm.slnx
```

You'll need:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Access to the `aetheric-forge/aetheric-runtime` submodule
- A configured OIDC client in the Forge Keycloak realm — see [`docs/operations/sso.md`](docs/operations/sso.md)

Then run the web app:

```sh
dotnet user-secrets set "Keycloak:ClientSecret" "<your-client-secret>" --project src/AethericGm.Web/AethericGm.Web.csproj
dotnet run --project src/AethericGm.Web/AethericGm.Web.csproj
```

The app is available at `https://localhost:7088`.

## Documentation

- [`docs/architecture/`](docs/architecture) — system boundaries, the rules-record model, rules-package acquisition, credential handling.
- [`docs/product/`](docs/product) — the backlog and versioned milestones (v0.1, v0.2, v0.3).
- [`docs/stories/`](docs/stories) — individual delivery stories with acceptance criteria, in delivery order.
- [`docs/operations/`](docs/operations) — deployment and SSO configuration.

## License

The Aetheric GM application license is still being finalized. The `runtime` submodule is covered by the Aetheric General License; Aetheric GM itself will move under that same license once it is subsumed into the Forge Campus, but is not yet formally licensed as a standalone project. Rules content distributed *with* this repository (e.g. the `rules-neutral` demonstration package under `rulesets/`) is original and unencumbered; licensed third-party rulesets are never bundled here and must be installed separately by an authorized user under their own license terms.
