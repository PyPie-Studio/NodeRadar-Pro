# NodeRadar Pro Changelog

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
