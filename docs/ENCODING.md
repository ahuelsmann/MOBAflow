# Encoding Policy

This repository standardizes on:

- **Encoding:** UTF-8 (without BOM)
- **Line endings:** CRLF

Rationale:

- Most modern .NET tooling and editors default to UTF-8 without BOM.
- Mixed BOM/no-BOM in XML/XAML can lead to intermittent parsing issues in parts of the toolchain.
- `.gitattributes` enforces **CRLF** consistently. Encoding is enforced via `.editorconfig`.

If a specific tool requires UTF-8 with BOM for a certain file type, add a dedicated `.editorconfig` override for that file type and document the reason here.
