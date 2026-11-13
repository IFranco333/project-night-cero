# Security policy

Noche Cero is an offline, single-player desktop game. It has no backend, no accounts and no network services in the slice.

## Threat model

The meaningful attack surface is therefore small:

| Surface | Risk | Mitigation |
|---|---|---|
| **Imported save files** | Malformed or malicious JSON causing crashes, hangs or unexpected deserialization | Typed schema validation, size limit (10 MB), numeric bounds (no NaN/Infinity), version check, checksum; no polymorphic/reflection-based deserialization |
| **Dependencies (NuGet)** | Vulnerable or unmaintained packages | Minimal dependencies in the core (zero runtime dependencies), `dotnet list package --vulnerable --include-transitive` before releases, versions pinned |
| **Build artifacts** | Tampered distributor binaries | Reproducible CI builds; exported builds signed when distributed |
| **Repository secrets** | Leaked tokens in history | Machine-specific settings are gitignored; history is reviewed before publication; leaked credentials are rotated, not just deleted |

## Non-goals

The slice has no anti-cheat requirements: players own their save files, and a modified save only affects their own game.

## Reporting a vulnerability

Open a private security advisory on the repository (GitHub → Security → Advisories) rather than a public issue. Include the version, the file that triggers the problem and, when possible, a minimal reproducer.

## Supported versions

Only the latest release receives security fixes.
