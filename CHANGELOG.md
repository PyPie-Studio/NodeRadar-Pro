# NodeRadar Pro Changelog

## NodeRadar Pro v2.0.4 (2026-09-29)

### User Interface and Vector Modernization
- **Project-Wide SVG Vector Standardization (`ThemeTokens.cs`)**: Replaced Unicode emojis across UI pages and notifications with technical SVG vector icons (`ThemeTokens.VectorIcon`), adding reusable vector constants (`SvgAlertTriangle`, `SvgInfo`, `SvgClose`, `SvgDownload`, `SvgBell`, `SvgFolder`, `SvgGlobe`, `SvgRadar`, `SvgGear`, `SvgRefresh`, `ButtonContent`).
- **Telemetry Stat Card Left-Alignment (`AlertsPage.cs`, `SystemLogsPage.cs`)**: Explicitly configured `HorizontalAlignment.Left` on SVG icons inside `MakeStatCard` and `MakeLogStatCard`, correcting centered icon drift in vertical stack panels and aligning with numerical metrics.
- **Dynamic Changelog Integration (`ChangelogService.cs`, `SupportPage.cs`)**: Embedded `CHANGELOG.md` directly into the assembly manifest (`NodeRadarPro.Resources.CHANGELOG.md`) with filesystem fallback. Converted `SupportPage.cs` to dynamically render recent release badges, dates and bullet items.
- **Device Inventory Filter Chip Responsive Wrapping (`InventoryPage.cs`)**: Replaced unconstrained horizontal `StackPanel` with `WrapPanel` and boundary clipping on filter chips, preventing chips from overflowing past container bounds on non-maximized viewports.

### Central Package Management (CPM) Upgrades
- **Avalonia Framework 12.1.3 (`Directory.Packages.props`)**: Upgraded `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent` and `Avalonia.Fonts.Inter` to 12.1.3.
- **Dependency Upgrades (`Directory.Packages.props`)**: Updated `MailKit` to 4.18.1, `Moq` to 4.21.0, `coverlet.collector` to 10.1.0 and `SonarAnalyzer.CSharp` to 10.35.0.4138.

### Test Coverage and Quality Gate
- **No-Emoji Regression Gate (`NoEmojiRegressionTests.cs`)**: Added automated regression test scanning all `.cs` files across `src/NodeRadarPro` to ensure zero Unicode emojis exist in the codebase.
- **Stat Card Alignment Verification (`StatCardAlignmentTests.cs`)**: Added unit tests verifying left-alignment across stat card icon factories.
- **Dynamic Changelog Unit Tests (`ChangelogServiceTests.cs`)**: Verified release parsing, version extraction and highlight bullet formatting.

## NodeRadar Pro v2.0.3 (2026-09-27)

### Bug Fixes & Application Resilience
- **Avalonia Resource URI Assembly Alignment (`DarkPurpleTheme.cs`, `SideNavBar.cs`, `SupportPage.cs`)**: Fixed desktop startup crash caused by legacy space in `avares://NodeRadar Pro/...` resource URIs following assembly migration to `NodeRadarPro`. Aligned all icon and logo URIs to `avares://NodeRadarPro/...`.
- **Defensive UI Asset Loading**: Wrapped window icon and navigation logo loading routines in `try/catch` handlers with stream disposal and warning telemetry, ensuring missing or corrupt media assets never terminate the host process.
- **Fallback Binary Verification Path (`DarkPurpleTheme.cs`)**: Corrected executable fallback probe in `VerifySystemIntegrity` from `NodeRadar Pro.dll` to `NodeRadarPro.dll`.

### Quality Gate & Diagnostics
- **Automated Embedded Asset Tests (`AssetLoadingTests.cs`)**: Added test fixtures verifying critical embedded UI assets (`.ico`, `.png`) load and open with valid byte streams via `StandardAssetLoader`. Added regression assertion ensuring whitespace in resource hostnames is caught.
- **Master Quality Gate Smoke Step (`Test-MasterGate.ps1`)**: Added Step 2/5 application startup smoke verification executing `--smoke-test` on every local quality gate run.
- **Release Packaging Preflight Check (`Build-ReleasePackage.ps1`)**: Added Step 2/3 startup smoke verification that validates published self-contained binaries prior to Inno Setup installer compilation.
- **Coverage Ratchet**: Ratcheted coverage threshold to 55.0% and deduplicated VSTest telemetry.

## NodeRadar Pro v2.0.2 (2026-09-27)

### Performance and Zero-Allocation Networking
- **Zero-Allocation Linux ARP Parsing (`ArpResolver.cs`)**: Replaced whole-file `ReadAllText` and `string.Split` with streaming `StreamReader`, `ReadOnlySpan<char>` slicing in `TryParseProcNetArpLine` and `string.Create` for MAC uppercase and hyphen substitution (73% memory allocation reduction, 3.7x execution speedup).
- **Zero-Allocation Subnet IP Parsing (`SubnetScanner.cs`)**: Replaced IP string splitting with stack spans, `CountDots` and .NET 10 `HashSet.TryGetAlternateLookup<ReadOnlySpan<char>>` (0 heap bytes allocated for IP comparisons during active discovery sweeps).
- **Event-Driven Log Queue Flushing (`Logger.cs`)**: Replaced `Thread.Sleep(10)` polling loop in `Logger.FlushForTesting()` with `ManualResetEventSlim` signaling (15x faster test queue flush latency).

### Architecture and UI Refactoring
- **Modularized Security Audit Export (`SupportPage.cs`)**: Refactored monolithic audit generator into modular static helpers (`BuildSecurityAuditReport`, `AppendExecutiveSummary`) with strict 12-hour AM/PM formatting (`yyyy-MM-dd hh:mm:ss tt`) and mirrored unit tests.
- **De-nested Theme Integrity Shield (`DarkPurpleTheme.cs`)**: Extracted anonymous `Task.Run` delegate into a clean static `VerifySystemIntegrity` method.
- **De-nested Inventory Fingerprinting (`InventoryPage.cs`)**: Extracted node fingerprinting logic in `OnPingClicked` into `TryFingerprintDiscoveredNodeAsync` with clean guard clauses.
- **Clean Database History Merging (`LocalDatabase.cs`)**: Flattened 7 levels of indentation in `MergeWithHistoryBulk` by extracting `MergeExistingNode` and `MergePortBanners` helpers.

### Security
- **SMTP Certificate Validation Hardening (`EmailService.cs`)**: Removed commented-out insecure SSL/TLS certificate validation bypass callback.
- **Pull Request Security Triage**: Audited 22 open pull requests against security invariants, closing unsafe contributions that stored plaintext master keys on disk, deleted live user databases in `%USERPROFILE%\Documents` during tests or invoked `rundll32.exe`.

### Test Coverage & Diagnostics
- **Comprehensive Database Backup Tests (`DatabaseBackupServiceTests.cs`)**: Added unit and integration tests covering backup creation, custom target paths, 7-generation retention pruning, corrupt database rotation and restore failure recovery.
- **Headless Audio Testing Seam (`AudioService.cs`)**: Added `SetSoundPlayerForTesting` test seam for deterministic test runs without OS audio hardware dependencies.
- **Inventory Uptime History Test (`LocalDatabaseTests.cs`)**: Added targeted unit test verifying MAC-filtered 24-hour uptime queries for selected inventory assets.
- **Ratcheted Coverage Gate (`coverage-threshold.txt`)**: Locked code coverage ratchet threshold to 55.0% across all 349 passing tests.

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
