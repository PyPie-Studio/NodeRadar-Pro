# NodeRadar Pro Architecture Decision Records (ADRs)

This document logs significant architectural, security, and quality decisions for **NodeRadar Pro**.

---

## ADR-001: Solution Hierarchy & Central Package Management (2026-08-17)
- **Status:** Accepted
- **Context:** Managing dependencies and project boundaries across the desktop client and test suites requires centralized versioning and strict separation of concerns.
- **Decision:** Configured `Directory.Build.props` and `Directory.Packages.props` for Central Package Management (CPM) with unified dependency resolution, deterministic compilation and automated analyzer enforcement across `src/NodeRadarPro` and `tests/NodeRadarPro.Tests`.

---

## ADR-002: Static Analysis Hardening & Compiler Warnings (2026-08-17)
- **Status:** Accepted
- **Context:** Enforce enterprise code health, detect socket/memory leaks, and ensure clean async patterns.
- **Decision:** Onboarded `Directory.Build.props` with `SonarAnalyzer.CSharp` (v10.32) and `Roslynator.Analyzers` (v4.16) alongside `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- **Suppression Baseline:** Created an onboarding `<NoWarn>` suppression baseline. The baseline is **shrink-only** — rules are permanently promoted to active enforcement as legacy debt is resolved.

---

## ADR-003: 12-Hour AM/PM Time Format Standard (2026-08-17)
- **Status:** Accepted
- **Context:** Ensure consistent user experience across telemetry logs, charts, and table grids.
- **Decision:** Standardized on 12-hour AM/PM format (`yyyy-MM-dd hh:mm:ss tt` or `hh:mm:ss tt`). 24-hour military time is prohibited in user-facing UI.

---

## ADR-004: Windows DPAPI & LiteDB Security Architecture (2026-08-17)
- **Status:** Accepted
- **Context:** Protect local database assets, SMTP credentials, and user configuration.
- **Decision:** Embedded LiteDB database is encrypted with AES-256. Database encryption keys and credentials are encrypted using `System.Security.Cryptography.ProtectedData` (Windows DPAPI scoped to CurrentUser).

---

## ADR-005: Inno Setup Dynamic Versioning, Mutex & Restart Manager (2026-08-21)
- **Status:** Accepted
- **Context:** Prevent version mismatch between compiled `.exe` metadata and installer packages, and ensure graceful process termination during in-place upgrades.
- **Decision:** Configured `GetFileVersion` in `Inno/installer.iss` to extract version automatically from the compiled binary. Synchronized named single-instance mutex `NodeRadarPro_App_Mutex_Active` in `Program.cs` and `Inno/installer.iss` alongside `CloseApplications=yes` and embedded `VersionInfo*` metadata.

