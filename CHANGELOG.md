# NodeRadar Pro Changelog



## NodeRadar Pro v1.6.0 (2026-08-30)

### Maintenance and Refactoring
- ≡ƒº╣ Remove MakeThresholdSliderCard wrapper and update call sites to use MakeSliderCard directly
- ≡ƒº╣ Refactor MakeThresholdSliderCard to reduce duplication
- ≡ƒº╣ refactor: reduce complexity in SettingsPage constructor
- ≡ƒº╣ [remove deprecated GuessDeviceType method]
- test: add unit tests for AlertTypeExtensions
- test: add unit tests for IntrusionDetector
- ≡ƒöÆ Replace System.Random with RandomNumberGenerator in NbnsProbe
- ≡ƒöÆ Fix command injection in AppUtils.OpenSafeUrl
- ≡ƒöÆ Enable online certificate revocation check in UpdateService

## NodeRadar Pro v1.6.0 (2026-08-30)

### Bug Fixes
- embed multi-resolution NodeRadar icon into setup installer

### Maintenance and Refactoring
- ≡ƒº╣ Remove MakeThresholdSliderCard wrapper and update call sites to use MakeSliderCard directly
- ≡ƒº╣ Refactor MakeThresholdSliderCard to reduce duplication
- ≡ƒº╣ refactor: reduce complexity in SettingsPage constructor
- ≡ƒº╣ [remove deprecated GuessDeviceType method]
- test: add unit tests for AlertTypeExtensions
- test: add unit tests for IntrusionDetector
- ≡ƒöÆ Replace System.Random with RandomNumberGenerator in NbnsProbe
- ≡ƒöÆ Fix command injection in AppUtils.OpenSafeUrl
- ≡ƒöÆ Enable online certificate revocation check in UpdateService
- ≡ƒº¬ Add comprehensive unit tests for SsdpDiscoveryMethod
- ≡ƒº╣ Consolidate duplicate catch blocks in SettingsPage
- chore(deploy): enhance git staging and gh release handling in deploy pipeline
- refactor(core): restructure solution topology into src and tests with 1:1 domain mirroring
- ≡ƒº¬ test: add unit tests for SsdpProbe
- test: add unit tests for DeepFingerprintEngine

## NodeRadar Pro v1.5.0 (2026-08-29)

### Architecture & Solution Restructuring
- Restructured solution into standard `src/NodeRadarPro/` and `tests/NodeRadarPro.Tests/` hierarchy
- Reorganized test suites with 1:1 domain directory mirroring across Core, Data, and UI layers
- Unified root application namespace to `NodeRadarPro` and removed MSBuild compile hacks
- Modernized Inno Setup installer compilation to eliminate deprecation warnings

### Maintenance and Refactoring
- Excised redundant `DiagnosticLogger` forwarding wrapper in favor of direct `Logger` invocations
- Synchronized all automation scripts (`Test-MasterGate.ps1`, `Build-ReleasePackage.ps1`, `Deploy-NodeRadar.ps1`, `Test-Coverage.ps1`, `Update-OuiDatabase.ps1`)
- Master Quality Gate passed with 100% test success (238/238 passing on xUnit v3)

## NodeRadar Pro v1.0.0 (2026-08-28)

### New Features
- Onboarded Central Package Management (CPM) via Directory.Packages.props across all projects
- Upgraded entire test suite to xUnit v3 with 263 passing unit and integration tests
- Built 7-phase automated release and deployment pipeline (deploy.bat)
- Automated GitHub Releases publishing with Inno Setup installer uploads
- Enhanced PortScanner with SemaphoreSlim(64) socket concurrency throttling and linked cancellation
- Enhanced UpdateService with JsonDocument parsing and Semantic Versioning
- Hardened system integrity SHA-256 self-audit with constant-time cryptographic verification

### Security Enhancements
- Added PBKDF2 + AES-GCM encryption fallback for credentials
- Added Authenticode and X509Chain certificate verification for update executables
- Fixed command injection vulnerabilities in URL opening and external process invocations

### Maintenance and Refactoring
- Enforced 100% local-first CI/CD pipeline with pre-push quality gate
- Cleaned up empty catch blocks and tightened Roslyn/Sonar analyzer compliance
- Added deterministic test session settings and Cobertura attribute filters
