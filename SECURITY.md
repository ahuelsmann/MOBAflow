# Security Policy

## Reporting a Vulnerability

Please report suspected security vulnerabilities through
[GitHub Private Vulnerability Reporting](https://github.com/ahuelsmann/MOBAflow/security/advisories/new).
This creates a private report that is visible only to the reporter and the
repository maintainers.

Do not disclose an unpatched vulnerability in a public issue, pull request,
discussion, or other public channel.

Include as much of the following information as practical:

- the affected component, version, tag, or commit;
- the required configuration and environment;
- clear reproduction steps or a minimal proof of concept;
- the observed and expected behavior;
- the potential impact, including hardware-control or local-network effects;
- relevant logs with credentials, tokens, personal data, and private network
  details removed; and
- any known mitigation or suggested fix.

If GitHub Private Vulnerability Reporting is unavailable, open a public issue
that asks the maintainers for a private security contact. Do not include
vulnerability details in that issue.

## Supported Versions and Scope

Security fixes target `main` and the latest tagged release when a supported
fix can be produced. Older branches and releases are maintained on a
best-effort basis.

The policy covers code and configuration shipped in this repository,
including:

- MOBAflow for Windows;
- MOBAsmart for Android;
- MOBApi and its REST and SignalR surfaces;
- Backend, Common, Domain, SharedUI, Sound, track-plan, and display libraries;
- Z21 and local-network communication; and
- MOBAdisplay ESP32 firmware maintained in this repository.

Vulnerabilities that originate exclusively in an upstream dependency should
normally also be reported to that dependency's maintainers. Report them here
when MOBAflow exposes or worsens the vulnerability, or when coordinated
mitigation is required in this repository.

## Safe Research

When investigating a potential vulnerability:

- use systems, accounts, networks, and model-railroad hardware that you own or
  are explicitly authorized to test;
- avoid accessing, changing, or retaining another person's data;
- avoid denial-of-service tests, destructive commands, unsafe train movement,
  or tests that could damage hardware;
- stop testing if you encounter secrets, personal data, or an unexpected
  safety impact; and
- collect only the evidence necessary to demonstrate the issue.

Good-faith research that follows these rules helps the project address risks
without endangering users, networks, or physical layouts.

## Handling and Disclosure

1. Maintainers aim to acknowledge a private report within three business days.
2. The report is reproduced and assessed for severity, affected versions, and
   immediate mitigations.
3. The reporter and maintainers coordinate remediation and an appropriate
   disclosure timeline.
4. A fix and user guidance are prepared before public disclosure whenever
   practical.
5. Reporter credit is included when requested and mutually agreed.

Response and remediation times depend on severity, reproducibility, hardware
requirements, and the affected platforms. Please keep the report private until
the maintainers confirm that coordinated disclosure is appropriate.

## Security Practices for Contributors

- Keep credentials, tokens, private keys, and device-specific Wi-Fi secrets
  out of source control.
- Keep tracked `MOBAflow/appsettings*.json` files free of secrets; use local
  configuration, User Secrets, or environment variables where supported.
- Never include secrets or unnecessary private network information in logs,
  tests, screenshots, issues, or pull requests.
- Rotate credentials used with real services or devices during testing.
- Treat MOBApi, Z21, discovery, provisioning, firmware, file, and script
  execution boundaries as security-sensitive.
- Update vulnerable dependencies promptly and describe security-relevant
  behavior changes and negative tests in pull requests.
