# Security Policy

## Reporting a Vulnerability
- Please report vulnerabilities privately. Do **not** open a public issue.
- Preferred: create a private Azure DevOps work item (Security category) assigned to the maintainer (@ahuelsmann).
- If Azure DevOps access is not available, contact the maintainer via Azure DevOps profile message.
- Include reproduction steps, affected components, and any exploit details. Avoid sharing secrets in reports.

## Scope
- Supported branches: `main` (current development). Older branches are maintained on a best-effort basis.
- Components: WinUI, MAUI, WebApp (Blazor), Backend, TrackPlan, SharedUI, Plugins shipped in the repo.

## Handling Process
1. We acknowledge reports within 3 business days.
2. We reproduce, assess severity, and prepare a fix/mitigation.
3. A patched build is produced and communicated before public disclosure.
4. Credits are given when desired by the reporter.

## Best Practices for Contributors
- Keep secrets out of source control (`appsettings*.json` should be local-only).
- Rotate keys after tests with real services (e.g., Azure Speech).
- Prefer relative paths for local assets and avoid embedding absolute user paths.
- Update vulnerable dependencies promptly and document security-impacting changes in PR descriptions.
