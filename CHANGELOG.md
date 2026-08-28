# NodeRadar Pro Changelog

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
