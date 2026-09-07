# Forge SSO configuration

Create a confidential OpenID Connect client named `aetheric-gm` in the target Forge realm.

For local development, allow these exact redirect URIs:

- `https://localhost:7088/signin-oidc`
- `https://localhost:7088/signout-callback-oidc`

Set the web origin to `https://localhost:7088`. Configure production redirect URIs explicitly; do not use wildcards.

The committed issuer is `https://sso.aethericforge.ca/realms/aethericforge.ca`. Its discovery document has been verified. Store the client secret outside the repository:

```sh
dotnet user-secrets set "Keycloak:ClientSecret" "REPLACE_WITH_SECRET" --project src/AethericGm.Web/AethericGm.Web.csproj
```

Override the issuer, realm, or client ID the same way when targeting another environment. Environment variables use double underscores, for example `Keycloak__ClientSecret`.
