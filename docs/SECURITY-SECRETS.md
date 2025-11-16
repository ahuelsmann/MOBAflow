# Security & Secrets Guidelines

## Key Principles
- **Do NOT commit secrets** (API keys, connection strings, speech keys) to source control.
- Use environment variables, user secrets (local dev), or a secrets store (Azure Key Vault) in CI/CD.

## Local Development
- Use `dotnet user-secrets` for local secrets with ASP.NET/CLI projects:
  - `dotnet user-secrets init`
  - `dotnet user-secrets set "Speech:Key" "<key>"`
- Alternatively, set environment variables on developer machines.

## CI/CD and Production
- Store secrets in CI secrets (Azure DevOps variable groups, GitHub Actions secrets) or Azure Key Vault.
- Grant minimal permissions and rotate keys regularly.
- Use managed identities where possible.

## Examples
- MAUI / WinUI: read keys from configuration or `Environment.GetEnvironmentVariable("SPEECH_KEY")`.
- Avoid `appsettings.json` for secrets in repo; use `appsettings.Production.json` populated at deploy time.

## Auditing & Incident Response
- Rotate compromised keys immediately.
- Audit access to secrets storage and CI logs.

---

Short and pragmatic rules to keep project secrets safe and reproducible.