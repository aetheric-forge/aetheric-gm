# Hosting GM on either Campus

GM's institution is the unit reused by the standalone GM Campus and the larger Forge
Campus. Do not embed the standalone web host or create a second root Campus inside Forge.
The same `AethericGmInstitution` type serves both hosts.

## Composition contract

Reference `AethericGm.Institutions.Gm` and `AethericGm.Core` from the host. Register the seven
GM capabilities required by `IAethericGm` as singleton-compatible services. To use the
existing adapters, reference `AethericGm.Infrastructure` and register:

```csharp
// The host supplies ISshPrivateKeyProtector using its chosen key protection setup.
services.AddLocalGmStorage(new LocalGmStorageOptions(absoluteDataPath, absoluteRulesPath));
```

Inside the host's existing Campus factory, after constructing the parent and its shared
institutions:

```csharp
campus.RegisterAethericGm(serviceProvider);
```

Expose the same registered instance to UI consumers after declaring the Campus factory:

```csharp
services.AddSingleton<IAethericGm>(sp =>
    sp.GetRequiredService<ICampus>().Resolve<IAethericGm>());
```

The standalone host exposes its instance through `AethericGmCampus.Gm` instead. Neither
route creates a second GM instance. Duplicate institutional registration is rejected by
runtime. The provider used for registration must outlive the institution; it supplies
singleton-compatible application capabilities, not request-scoped user state.

Before accepting requests, call `InitializeLocalGmStorageAsync` when using those adapters.
The host also owns GM initialization/startup/shutdown. Keep the parent available until GM
has stopped; do not rely on runtime `InstitutionBase` to traverse registered children.

## Moving an existing local installation

Keep the SQLite database, installed `RulesPackages` directory, built-in rules catalog,
and credential-protection keys available at the host's explicit locations. With the same
adapters and paths, changing Campus does not change campaign IDs, rules references, or
schema. For an actual server move, stop writers and take a consistent SQLite backup;
this refactor does not perform a live data transfer.

Existing encrypted SSH credentials require the same Data Protection key ring, application
name (`AethericGm`), and protector purpose. Do not silently switch protection settings when
bringing the institution into the larger host.

## Web integration still owned by the destination host

The Blazor pages and layout currently live in `AethericGm.Web`, following the separation
between host UI and reusable services seen in aetheric-web/Parallel You. An integration
will move/adapt those pages or extract a Razor class library. The standalone app's `/`,
`/profile`, `/characters`, and rules routes, login/logout endpoints, and CSS must be mounted
or adapted to the Forge host rather than importing `Program.cs`. UI-only session state
(`DiceTrayState`) stays scoped to the circuit. The host supplies authenticated subject IDs;
GM does not install a second SSO configuration.

The current repositories still have single-operator campaign/selection semantics. Mounting
GM beneath the public Forge Campus does not itself make shared campaign data multi-user safe.
Define that product boundary before exposing it to multiple operators.

Align the runtime dependency across projects at integration time. This GM checkout pins
`ca90169` on v2.0; the inspected aetheric-web checkout pins `cb7f93b`. Do not ship duplicate
runtime assemblies or edit the upstream submodule to make an application-specific shortcut.

## Development rules

New game capabilities belong in Core and the GM institution; concrete persistence adapters
belong in Infrastructure. New institutional capabilities use runtime contracts and services.
Configuration, provider selection, paths, authentication, and Campus creation belong to the
host. A new feature should not require a reference from the portable institution to Web.

`GmInstitutionTests` exercises rehosting against another real runtime Campus with the same
SQLite data, inherited Registry resolution, duplicate registration protection, and the
absence of Web/Infrastructure assembly dependencies from the institution.
