# NodeRadar Pro Changelog

## NodeRadar Pro v2.0.1 (2026-09-20)

### Packaging & Installation
- Added upfront administrative elevation (`PrivilegesRequired=admin`) in Inno Setup installer.
- Configured frictionless upgrades with automatic directory page bypassing (`DisableDirPage=auto`) and duplicate directory warning suppression (`DirExistsWarning=no`).
- Registered application icon for Windows Installed Apps (`UninstallDisplayIcon`).
- Purged bytecode obfuscation tooling to provide transparent open-source binaries.

### Maintenance & Code Health
- Switched Support Page updates to a direct 1-click link opening official GitHub releases in the default web browser.
- Purged legacy client-side auto-update download routines, temporary script runners and Authenticode certificate chain checks.
- Removed unused startup update checks and ghost settings from application configuration.
- Linked UI version telemetry dynamically to assembly metadata to eliminate version drift.

## NodeRadar Pro v2.0.0 (2026-09-19)

### Open-Source Transition & Licensing
- Transitioned NodeRadar Pro to open-source software under the permissive MIT License (Copyright 2026 PyPie Studio).
- Removed legacy closed-source EULA and established open development governance.
- Adopted the Contributor Covenant v2.1 Code of Conduct (`CODE_OF_CONDUCT.md`).

### Diagnostic & Automation Tooling
- Added `Test-NetworkEnvironment.ps1`: Automated pre-flight network stack diagnostics checking Win32 `SendARP` API binding, user-mode ICMP echo sockets, network adapter precedence (physical LAN vs virtual/VPN adapters), Windows Defender Firewall UDP multicast rules (`5353` and `1900`) and storage permissions.
- Added `Invoke-MasterCommit.ps1`: Automated developer workflow script enforcing conventional commits, running the Master Quality Gate, synchronizing AST knowledge graphs and staging changes.
- Added `Get-CoverageRate.ps1` and ratcheted line test coverage threshold to `52.0%` in `coverage-threshold.txt`.

### Technical Documentation & Architecture Records
- Added `docs/benchmarks.md`: Empirical discovery sweep latency benchmarks (/24 subnet), memory utilization profiles (72 MB idle, 124 MB peak), SkiaSharp 144 Hz frame render budgets (1.85 ms/frame) and LiteDB batch transaction speeds.
- Added `docs/decisions.md` records:
  - **ADR-006**: Win32 `SendARP` & Native IP Helper API vs Kernel Drivers (user-mode Layer 2 discovery without Npcap or elevation).
  - **ADR-007**: `SemaphoreSlim` Socket Throttling & Ephemeral Port Exhaustion (limiting concurrent in-flight sockets to prevent `WSAENOBUFS 10055`).
  - **ADR-008**: Heuristic Multi-Layer Device Classification (prioritizing mDNS/SSDP/WS-Discovery over transport ports and MAC OUI database).
- Added `docs/troubleshooting.md`: Field guide resolving virtual adapter route metric precedence (Hyper-V, WSL2, VMware), ICMP drops vs ARP visibility on host firewalls, Wi-Fi client isolation (AP isolation), 802.11 DTIM power-save jitter spikes and VPN split-tunneling route conflicts.

### CI/CD Pipeline & Code Health
- Added `.github/workflows/release.yml`: Automated tag-triggered release pipeline compiling Inno Setup installers, packaging portable zip archives, generating cryptographic `SHA256SUMS.txt` checksums and publishing GitHub Releases.
- Added `.github/dependabot.yml`: Weekly automated dependency vulnerability scanning for NuGet and GitHub Actions.
- Hardened Roslyn and Sonar analyzer rules across all projects, silenced IDE0130 folder namespace mismatches and verified 324/324 passing tests on xUnit v3.

## NodeRadar Pro v1.6.0 (2026-08-30)

### Security Enhancements
- Enforced strict online certificate revocation checking (`X509RevocationMode.Online`) in `UpdateService` without insecure fallbacks.
- Eliminated command injection vector in `AppUtils.OpenSafeUrl` via direct `UseShellExecute = true` Windows protocol handling.
- Upgraded NetBIOS (NBNS) transaction ID generation to cryptographically secure `RandomNumberGenerator.Fill`.

### Refactoring and UI Improvements
- Modularized monolithic `SettingsPage` constructor into dedicated private builder methods (`BuildHeaderSection`, `BuildGeneralSettingsCard`, etc.).
- Refactored `MakeThresholdSliderCard` into `MakeSliderCard` with unified styling parameters and eliminated card layout duplication.
- Pruned obsolete `VendorLookup.GuessDeviceType` method and synchronized OUI database code generator.
- Resolved CA1307 string comparison warning in `VendorLookup` with zero-allocation char replacement.

### Test Coverage and Diagnostics
- Added comprehensive unit tests for `IntrusionDetector` with constructor DI seams and isolated in-memory LiteDB fixtures.
- Added parameterized unit tests for `AlertTypeExtensions` display names and icon mappings.
- Added comprehensive unit test suites for `SsdpDiscoveryMethod`, `SsdpProbe` and `DeepFingerprintEngine`.

## NodeRadar Pro v1.5.0 (2026-08-29)

### Architecture and Solution Restructuring
- Restructured solution into standard `src/NodeRadarPro/` and `tests/NodeRadarPro.Tests/` hierarchy.
- Reorganized test suites with 1:1 domain directory mirroring across Core, Data and UI layers.
- Unified root application namespace to `NodeRadarPro` and removed MSBuild compile hacks.
- Modernized Inno Setup installer compilation to eliminate deprecation warnings.

### Maintenance and Refactoring
- Excised redundant `DiagnosticLogger` forwarding wrapper in favor of direct `Logger` invocations.
- Synchronized all automation scripts (`Test-MasterGate.ps1`, `Build-ReleasePackage.ps1`, `Deploy-NodeRadar.ps1`, `Test-Coverage.ps1`, `Update-OuiDatabase.ps1`).
- Master Quality Gate passed with 100% test success (238/238 passing on xUnit v3).

## NodeRadar Pro v1.0.0 (2026-08-28)

### New Features
- Onboarded Central Package Management (CPM) via Directory.Packages.props across all projects.
- Upgraded entire test suite to xUnit v3 with 263 passing unit and integration tests.
- Built 7-phase automated release and deployment pipeline (`deploy.bat`).
- Automated GitHub Releases publishing with Inno Setup installer uploads.
- Enhanced PortScanner with SemaphoreSlim(64) socket concurrency throttling and linked cancellation.
- Enhanced UpdateService with JsonDocument parsing and Semantic Versioning.
- Hardened system integrity SHA-256 self-audit with constant-time cryptographic verification.

### Security Enhancements
- Added PBKDF2 + AES-GCM encryption fallback for credentials.
- Added Authenticode and X509Chain certificate verification for update executables.
- Fixed command injection vulnerabilities in URL opening and external process invocations.

### Maintenance and Refactoring
- Enforced 100% local-first CI/CD pipeline with pre-push quality gate.
- Cleaned up empty catch blocks and tightened Roslyn/Sonar analyzer compliance.
- Added deterministic test session settings and Cobertura attribute filters.
