# NodeRadar Pro Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.0] - 2026-10-25

### Added
- **Keep a Changelog 1.1.0 Standardization (`CHANGELOG.md`, `ChangelogService.cs`, `SupportPage.cs`, `ChangelogServiceTests.cs`)**: Retrospectively reformatted all historical releases into strict Keep a Changelog 1.1.0 specification with `## [X.Y.Z] - YYYY-MM-DD` headers, standard category sections, diff comparison links, category-aware parsing in `ChangelogService`, and colored category badge pills on the desktop Support page.
- **Test Suite Mirroring and Headless UI Coverage Expansion (`NodeRadarPro.Tests.csproj`, `test.runsettings`, `artifacts/test-results/`)**: Removed `<SelfContained>true</SelfContained>` from `NodeRadarPro.Tests.csproj` to eliminate 510MB runtime bundling overhead on test builds. Mirrored test project folder structure 1:1 against production directories. Added headless UI component tests for `PortScansPage`, `SystemLogsPage`, `TraceroutePage`, `NavigationBars`, `MainWindowShell` and `InventoryControlTests`. Configured unified test output under `artifacts/test-results/`.
- **Host Process and Speaker Isolation (`AppUtils.cs`, `AudioService.cs`, `AppUtilsTests.cs`, `AudioServiceTests.cs`, `SupportPageAuditReportTests.cs`)**: Introduced injectable launcher delegates (`UrlLauncherForTesting`, `FolderLauncherForTesting`) in `AppUtils` and isolated audio alerts in `AudioServiceTests` using mock sound players, preventing test executions from launching browser URLs, opening explorer windows or chiming host hardware speakers.
- **Network Engine Latency Elimination and Offline Test Seams (`ConnectivityMonitor.cs`, `TracerouteEngine.cs`, `SubnetScanner.cs`, `ScannerDiagnostics.cs`, `PortScannerTests.cs`, `ArpResolver.cs`)**: Decoupled `ConnectivityMonitor` and `TracerouteEngine` from real socket I/O by injecting mock ping, ARP and reverse DNS providers. Injected `ITcpPortProber` into `SubnetScanner` to verify stealth port fallback probes, added `ScanAllSubnets` flag and injected test scanners in `ScannerDiagnostics` to eliminate live LAN sweeps. Reduced loopback probe timeout to 50ms in `PortScannerTests` and parameterized NetBIOS timeout to 25ms in `ArpResolverTests`, cutting test suite runtime from 14s to 4s.
- **Intrusion Detection and Network Sweep Verification (`IntrusionDetector.cs`, `SubnetScanner.cs`, `LocalDatabase.cs`, `SsdpDiscoveryMethod.cs`, `DeviceClassifierEngine.cs`)**: Added `BaseIpOverride` property to `IntrusionDetector` and added full sweep execution tests asserting LiteDB alert persistence with real `SubnetScanner` instances. Added multi-tier cascade verification for `SubnetScanner.QuickProbeAsync` across ARP, ICMP and TCP phases. Added deduplication via `.DistinctBy` in `LocalDatabase.MergeWithHistoryBulk` and atomic MAC tracking in `SubnetScanner` sweeps. Extracted `ParseSsdpResponse` in `SsdpDiscoveryMethod` for buffer parsing tests. Unified mobile brand detection in `DeviceClassifierEngine` across all 18 manufacturers.
- **Corrupt Database Rotation and Key Regeneration (`LocalDatabase.cs`, `CredentialVault.cs`, `LocalDatabaseTests.cs`, `CredentialVaultTests.cs`)**: Enclosed schema initialization within constructor recovery blocks to trigger automatic corrupt database rotation on malformed LiteDB files. Added direct unit tests for plaintext password migration, batch device deletion and PBKDF2/AES-GCM key regeneration.
- **Decoupled CSV Export Service (`CsvExportService.cs`, `SystemLogsPage.cs`, `ScannerPage.cs`, `SystemLogsExportTests.cs`)**: Extracted static `CsvExportService` with RFC 4180 escaping and mandatory 12-hour AM/PM formatting (`yyyy-MM-dd  hh:mm:ss tt`), removing duplicated CSV formatting routines from UI pages.
- **Avalonia Headless UI and Time Format Regression Tests (`ScannerPageHeadlessTests.cs`, `InventoryPageHeadlessTests.cs`, `TimeFormatRegressionTests.cs`)**: Replaced direct cursor construction with headless-safe `ThemeTokens.HandCursor` across UI pages, enabling headless component testing of card rendering, search filtering and subnet validation. Introduced architectural regression test verifying zero 24-hour military time formats and strict LTR layout across all solution source files.
- **Code Coverage Ratchet Gate (`coverage-threshold.txt`, `Test-MasterGate.ps1`)**: Ratcheted the local CI line coverage gate threshold from 67.50% to 73.00% (73.9% measured across 683 passing tests), locking in coverage gains from network socket abstractions and headless UI component suites.
- **Win32 IP Helper Table Query Seam (`ArpResolver.cs`, `ArpResolverTests.cs`)**: Added `GetIpNetTableDelegate` delegate seam defaulting to native Win32 `GetIpNetTable` P/Invoke. Enabled simulated full-table ARP queries without host OS networking dependencies, asserting loopback, multicast, broadcast filtering and cache bypass behaviors across 5 dedicated unit tests.
- **UDP Multicast Socket Seam for mDNS and SSDP (`IMulticastSocket.cs`, `DefaultMulticastSocket.cs`, `MdnsDiscoveryMethod.cs`, `SsdpDiscoveryMethod.cs`, `MdnsProbe.cs`, `SsdpProbe.cs`)**: Extracted `IMulticastSocket` interface and implemented `DefaultMulticastSocket` wrapper around `UdpClient`. Parameterized factory delegates across discovery engines and fingerprinting probes, enabling deterministic datagram testing of broadcast listeners without binding physical host sockets.
- **Avalonia Headless Coverage for Secondary UI Views (`SettingsPage.cs`, `AlertsPage.cs`, `DashboardPage.cs`, `SettingsPageHeadlessTests.cs`, `AlertsPageHeadlessTests.cs`, `DashboardPageHeadlessTests.cs`)**: Extended headless component test coverage across Settings, Alerts and Dashboard views. Added database-backed initial state refresh calls in `SettingsPage` and `AlertsPage` constructors, switched prune completion handling to non-blocking UI thread callbacks, and verified filter toggles, export interactions, threshold controls and stats synchronization.
- **Open PR Triage and Database / Engine Unit Test Expansion (`LocalDatabaseTests.cs`, `ConnectivityMonitorTests.cs`, `TracerouteEngineTests.cs`)**: Triaged open PR backlog, closing 4 defective PRs (#101, #99, #98, #95) per failure pattern rules. Rebased and squash-merged PR #96, #97, and #100, integrating 9 net new unit tests covering `LocalDatabase.MergeWithHistoryBulk` (empty, invalid MAC, update, and mixed batches), `ConnectivityMonitor.GetAllDevices` (tracking, removal, and defensive copying immutability), and `TracerouteEngine.ProbeHopAsync` (exception resilience without unmocked DNS socket operations), raising test suite coverage to 698 tests.
- **Destructive Action Safety Latches (`SettingsPage.cs`, `SystemLogsPage.cs`, `AlertsPage.cs`)**: Added 2-step confirmation latches with field-tracked automatic timers on destructive operations including factory resets, backup restorations, log purging and alert clearing. Replaced text unicode glyphs with vector icons and direct text states.
- **UI/UX Token Centralization and Vector Icons (`ThemeTokens.cs`, `SideNavBar.cs`, `TopNavBar.cs`)**: Added centralized SVG icon definitions (`SvgTraceroute`, `SvgInventory`, `SvgPortScans`, `SvgLogs`, `SvgSupport`) and acrylic box shadows (`ShadowSidebar`, `ShadowTopBar`). Enabled keyboard navigation (`Focusable = true`, Enter/Space key triggers) on sidebar navigation items.
- **Manual Device IP Entry and Search Ergonomics (`InventoryPage.cs`)**: Added editable IP text box for manual device entries (`ManualDevicePrefix`) with IPv4 validation, Enter-to-save trigger, and direct IP synchronization for ping and port sweep actions. Enforced 12-hour AM/PM formatting (`hh:mm tt`) on time axis labels and detailed tooltips (`yyyy-MM-dd hh:mm:ss tt`) on last scan timestamps. Wired Escape key to clear search filters.
- **Subnet Prefix Validation and Keyboard Execution (`ScannerPage.cs`, `TraceroutePage.cs`)**: Added IPv4 subnet prefix validation ensuring start and end IPs share the same `/24` prefix with start host octet less than or equal to end host octet. Wired Enter key listeners on target inputs.

### Changed
- **Master Quality Gate Coverage Integration and Ratchet (`Test-MasterGate.ps1`, `coverage-threshold.txt`)**: Integrated automated code coverage collection and threshold evaluation directly into `-WithTests` test runs with live summary telemetry, and ratcheted the line coverage threshold gate from 73.50% to 79.00%.
- **Avalonia UI Package Bump (`Directory.Packages.props`)**: Updated Avalonia UI, Avalonia.Desktop, Avalonia.Themes.Fluent and Avalonia.Fonts.Inter dependencies from 12.1.4 to 12.1.5.
- **Application Shell Isolation (`MainWindowShell.cs`, `App.axaml.cs`)**: Extracted window creation, top navigation, sidebar, page switching and backup timer lifecycle out of `DarkPurpleTheme.cs` into `NodeRadarPro.UI.Shell.MainWindowShell`. Retired obsolete `DarkPurpleTheme.cs`.
- **Composite Inventory Page Controls (`InventoryFilterBar.cs`, `InventoryGridControl.cs`, `InventoryDetailsFlyout.cs`, `InventoryPage.cs`)**: Decomposed monolithic 1,640-line `InventoryPage.cs` into standalone custom controls (`InventoryFilterBar`, `InventoryGridControl` and `InventoryDetailsFlyout`) under `NodeRadarPro.UI.Controls.Inventory`, isolating filter chips, datagrid rendering and device detail drawer state.
- **Source-Generated Win32 P/Invoke Migration (`ArpResolver.cs`, `NodeRadarPro.csproj`)**: Migrated native Win32 `SendARP` and `GetIpNetTable` P/Invoke declarations from runtime `[DllImport]` to compile-time source-generated `[LibraryImport]`, eliminating SYSLIB1054 interop marshalling overhead and enabling `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`.
- **Multicast Socket Migration (`IMulticastSocket.cs`, `DefaultMulticastSocket.cs`, `MdnsProbe.cs`, `SsdpProbe.cs`, `ScannerDiagnostics.cs`)**: Relocated `IMulticastSocket` and `DefaultMulticastSocket` into `NodeRadarPro.Core.Networking.Sockets`.
- **Subnet Scanner Abstractions Decomposition (`IPingProvider.cs`, `PingReplyWrapper.cs`, `DefaultPingProvider.cs`, `IArpResolver.cs`, `DefaultArpResolver.cs`, `ITcpPortProber.cs`, `DefaultTcpPortProber.cs`, `SubnetScanner.cs`)**: Split monolithic `ISubnetScannerDependencies.cs` into separate interface and default implementation files under `NodeRadarPro.Core.Engines.Abstractions`.
- **Generated Vendor Lookup Relocation (`VendorLookup.cs`, `Update-OuiDatabase.ps1`)**: Moved generated IEEE OUI table from `Data/VendorLookup.cs` to `Data/Generated/VendorLookup.cs` and updated `scripts/Update-OuiDatabase.ps1` to target the generated directory.
- **1:1 Folder-to-Namespace Hierarchy and IDE0130 Enforcement (`Directory.Build.props`, `.editorconfig`)**: Aligned namespaces across all solution files to mirror folder paths (`NodeRadarPro.Core.Engines`, `NodeRadarPro.Core.Models`, `NodeRadarPro.Core.Services`, `NodeRadarPro.UI.Pages`, `NodeRadarPro.UI.Controls`, `NodeRadarPro.UI.Navigation`, `NodeRadarPro.UI.Theme` and `NodeRadarPro.UI.Shell`). Removed `IDE0130` from `<NoWarn>` in `Directory.Build.props` and set `dotnet_diagnostic.IDE0130.severity = warning` in `.editorconfig`.
- **Range Sweep Post-Sweep Concurrency Throttling (`SubnetScanner.cs`)**: Added `SemaphoreSlim(32)` throttling across post-sweep ARP table resolution tasks in `ScanRangeAsync`, aligning range scans with subnet sweep thread ceilings.
- **SSDP URL Deduplication and Request Throttling (`SsdpProbe.cs`)**: Added `ConcurrentDictionary<string, byte>` URL tracking and a `SemaphoreSlim(8)` ceiling for XML device descriptors, eliminating redundant HTTP traffic across UPnP advertisement bursts.
- **Socket and Protocol Cancellation Propagation (`EmailService.cs`, `WakeOnLan.cs`)**: Propagated `CancellationToken` timeouts across SMTP socket operations and Magic Packet UDP broadcasts with automatic 15-second and 5-second ceilings.
- **Direct Connection Mode and Schema Indexing (`LocalDatabase.cs`, `DatabaseBackupService.cs`)**: Switched LiteDB connection mode to `Connection=direct;`, boosting write throughput by 97.5x (from 4,289ms to 44ms for 300 records). Added B-tree index on `IsRegistered` in `InitializeSchema()` to eliminate collection scans and removed dead compound index `Mac_Time` to eliminate 31.5% disk bloat.
- **Temporal Uptime Downsampling and UI Telemetry Alignment (`LocalDatabase.cs`, `InventoryPage.cs`, `DarkPurpleTheme.cs`, `AlertsPage.cs`)**: Added linear downsampling in `GetUptimeHistory` (`maxPoints = 120`) to bucket timeline intervals and compute average latency and aggregate online status. Offloaded `RefreshUptimeChart` to background worker tasks in `InventoryPage.cs`. Removed duplicate 24-hour `DeviceSelected` handler in `DarkPurpleTheme.cs` that clobbered 7-day and 30-day chart views. Updated `AlertsPage.cs` to query active alerts directly without masking by resolved entries.
- **Toolchain Alignment and Test Session Timeout Guard (`global.json`, `NodeRadarPro.Tests.csproj`, `test.runsettings`)**: Added `global.json` pinning .NET 10 SDK version 10.0.403 (`rollForward: latestFeature`) across dev workstations and CI runners. Bound `test.runsettings` via `RunSettingsFilePath` property in `NodeRadarPro.Tests.csproj` to enforce 5-minute session timeouts (`TestSessionTimeout: 300000`) and guard against hung network sockets.
- **Master Quality Gate Parity, Results Directory Unification and Collapsible Logging (`Test-MasterGate.ps1`)**: Wired `--settings test.runsettings` into test invocations, unified test results directory and coverage directory to eliminate duplicate `--results-directory` argument collisions under `dotnet test` and added GitHub Actions log group folding (`::group::` / `::endgroup::`) to structure runner logs into expandable step sections.
- **Remote CI Pipeline Unification and Step Summaries (`.github/workflows/ci.yml`)**: Delegated CI execution directly to `./scripts/Test-MasterGate.ps1 -WithTests -CheckCoverage -TestResultsDirectory "test-results"`, establishing single source of truth parity across local development and GitHub Actions. Added job timeout of 15 minutes, workflow concurrency cancellation for pull request branches, Central Package Management NuGet caching via `Directory.Packages.props`, TRX XML test summary reporting in `$GITHUB_STEP_SUMMARY` with null-safe error message extraction and test artifact archiving.
- **Dependabot Update Isolation and Automated Merge Safety (`.github/dependabot.yml`, `.github/workflows/dependabot-auto-merge.yml`)**: Decoupled NuGet dependencies into safe (minor and patch) and major update groups. Pinned `dependabot/fetch-metadata` to commit SHA `dbb9949bd7679ad46a2a0a256b107e3240e945c1` (`v2.3.0`) and added conditional checks preventing automated merges of major version updates.
- **Pre-Push Test Suite Assertion and Deployment Optimization (`scripts/Invoke-MasterCommit.ps1`, `scripts/Deploy-NodeRadar.ps1`)**: Enforced `-WithTests` assertion whenever `-Push` is passed to `Invoke-MasterCommit.ps1`. Added automated Roslyn formatting for generated `VendorLookup.cs` after IEEE OUI database sync, added `--no-build` flag to test runs in deployment phase 3 and added `--no-verify` flag to git push and tag push in phase 7 to eliminate redundant gate runs.
- **Release Packaging Cache and Installer Artifact Alignment (`.github/workflows/release.yml`)**: Added NuGet dependency caching to release verification and packaging jobs. Added Inno Setup 7 folder caching across both 64-bit and 32-bit program directories via `actions/cache@v4` with dynamic PATH configuration and null-safe TRX error parsing. Corrected installer artifact filename in release notes template to `NodeRadar Pro_v${version}_Setup.exe`.
- **ARP-First Sweep Ordering and Dead-Host SendARP Bypass (`SubnetScanner.cs`)**: Checked the operating system ARP cache (`TryGetMacFromArpTable`) before dispatching ICMP pings, bounded ping timeout to 400ms (150ms in FastScanMode), bypassed Win32 `SendARP` execution on dead hosts and removed redundant NetBIOS queries in `ResolveNodeMetadataAsync` to eliminate duplicate UDP probes.
- **SNMP Port Gating and Probe Timeout Ceiling (`SnmpProbe.cs`, `DeepFingerprintEngine.cs`)**: Skipped UDP querying in `SnmpProbe` only when scanned open ports are present and omit 161 and 162, reduced per-community timeout to 400ms and capped the per-probe execution ceiling in `DeepFingerprintEngine` to 1500ms to eliminate sweep tail latency.
- **Connectivity Monitor Semaphore Throttling (`ConnectivityMonitor.cs`)**: Enforced a `SemaphoreSlim(32)` concurrency ceiling across device connectivity checks in `CheckAllDevicesAsync`, preventing socket exhaustion and ephemeral port contention under network degradation.
- **Zero-Allocation MAC Vendor Lookup (`VendorLookup.cs`)**: Implemented `Dictionary.AlternateLookup<ReadOnlySpan<char>>` and `stackalloc char[8]` MAC address normalization in `GetVendor`, eliminating string heap allocations during manufacturer resolution.
- **Bounded Port Sweep Concurrency (`PortScansPage.cs`)**: Replaced `Enumerable.Range` task array allocations with `Parallel.ForEachAsync` throttled to 64 concurrent tasks, preventing heap memory spikes during 65,535-port scans.
- **Search Input Debounce and Filter Chip Retention (`InventoryPage.cs`, `SystemLogsPage.cs`)**: Added 200ms `DispatcherTimer` debouncing to search input text changed handlers, refreshing immediately on cleared text and preserving existing filter chip controls during filtering to eliminate visual tree rebuilds.
- **Visual Tree Lifecycle Gating (`RadarCanvas.cs`)**: Added `OnAttachedToVisualTree` and `OnDetachedFromVisualTree` overrides to start and stop the 60 FPS animation timer dynamically when the radar control attaches or detaches.
- **Startup Telemetry Deferral (`AlertsPage.cs`, `SystemLogsPage.cs`)**: Removed synchronous `RefreshAlerts` and `RefreshLogs` invocations from page constructors, deferring database queries and row construction until the user navigates to those tabs.
- **Developer Tools Reference and Probe Complexity Decomposition (`NodeRadarPro.csproj`, `MdnsProbe.cs`, `SsdpProbe.cs`)**: Removed restrictive configuration conditions from `AvaloniaUI.DiagnosticsSupport` in `NodeRadarPro.csproj` to eliminate Roslyn CS1061 errors during IDE design-time evaluation. Decomposed `StartSweepAsync`, `ParseTxtRecords` and `ExtractMdnsInstanceName` in `MdnsProbe.cs` and `StartSweepAsync` in `SsdpProbe.cs` into modular static helpers with table-driven signatures and span-based byte pattern matching, reducing cognitive complexity (Sonar rule S3776) below the threshold of 15.
- **Local Database Cognitive Complexity Decomposition and Zero-Allocation Downsampling (`LocalDatabase.cs`, `LocalDatabaseTests.cs`)**: Decomposed `MergeExistingNode` into discrete private static helpers (`CopyUserMetadata`, `InheritDeviceAttributes`, `MergeOpenPorts`, `UpdateExistingNodeState` and `SyncThreatAssessment`), reducing cognitive complexity from 29 to 0. Decomposed `DownsampleSnapshots` into lightweight stack-allocated primitives (`BucketContext`, `BucketAccumulator`, `GetBucketEnd`, `TryCreateBucketSnapshot` and `AlignTimeKinds`), eliminating Sonar rule S3776 and S107 violations, aligning DateTimeKind comparisons between UTC queries and LiteDB deserialized local timestamps and adding regression test coverage.
- **Connectivity Monitor Cognitive Complexity Decomposition (`ConnectivityMonitor.cs`)**: Refactored `UpdateTrackedDevices` and `CheckAllDevicesAsync` to eliminate Roslyn Sonar rule S3776 violations. Decomposed monolithic probe execution and dictionary update lambdas into modular private helpers (`UpdateTrackedDevice`, `MergeDeviceProperties`, `PreferNonEmpty`, `CheckDeviceAsync`, `ProbeDeviceConnectivityAsync`, `CheckArpStatusAsync`, `ResolveArpWithThrottlingAsync`, `CheckIcmpStatusAsync`, `ApplyProbeResult`, `RecordUptimeSnapshot`, `EvaluateAlerts`, `CheckConnectionLost`, `CheckReconnection`, `CheckHighLatency`, `CheckPacketLoss`, `TryConsumeAlertCooldown` and `PersistAlertsAndSnapshots`), reducing peak cognitive complexity from 67 to 4 while preserving thread safety and probe semantics.
- **Canvas Rendering Optimization and Timer Visibility Lifecycle (`RadarCanvas.cs`, `ThemeTokens.cs`)**: Overrode `OnPropertyChanged` on `RadarCanvas` and `DashboardPage` to automatically pause and resume the radar animation timer when visibility changes. Replaced per-frame string allocation keys with `NodeTextKey` struct in the formatted text cache, cached zoom overlay text, wrapped button cursors in headless-safe `HandCursor`, and standardized typography to `ThemeTokens.DefaultFont`.
- **Discrete Port Profiles and Concurrency Throttling (`PortScansPage.cs`)**: Capped active socket sweeps to 64 concurrent tasks (`SemaphoreSlim(64)`). Configured discrete port presets for Web (80, 443, 8080, 8443) and Database (1433, 1521, 3306, 5432, 6379, 27017), dynamically setting `_progressBar.Maximum = totalPorts` to accurately reflect sweep progress.
- **Roslyn and Sonar Analyzer NoWarn Retirement (`Directory.Build.props`)**: Retired S1144 (unused private members) and RCS1213 (unused member declarations) from the NoWarn shrink-only suppression debt ledger, enforcing strict dead-code hygiene in compiler builds.
- **Port Scanner OS Signature Deduplication (`PortScanner.cs`)**: Consolidated operating system port detection heuristics by eliminating redundant shadowed branch evaluations for macOS, Windows and Linux / Unix.
- **UI Control Button Field Scope Optimization (`InventoryDetailsFlyout.cs`, `InventoryFilterBar.cs`)**: Converted button references (`_saveBtn`, `_pingBtn`, `_wakeBtn` and `_portScanBtn` in `InventoryDetailsFlyout` and `_addBtn` in `InventoryFilterBar`) from instance fields to local variables in constructors, complying with Sonar rule S1450.
- **Shell Startup Parameter and Discard Hygiene (`MainWindowShell.cs`, `App.axaml.cs`, `PortScansPage.cs`, `InventoryPage.cs`)**: Pruned unused `mainWindow` parameter from `MainWindowShell.OnStartup`, removed unused `ip` parameter from `PortScansPage.MakeResultRow`, replaced unused lambda event arguments with discards, discarded unused `LocalDatabase.Instance` assignment in `App.axaml.cs` and collapsed duplicate fingerprint local variable assignments.

### Fixed
- **Synchronized Ping Ring Buffer (`NetworkNode.cs`)**: Encapsulated `PingHistory` with internal `System.Threading.Lock` monitor and snapshot collection copy on retrieval, synchronizing `RecordPing` and `PacketLossPct` to eliminate `InvalidOperationException` collection mutation crashes during concurrent connectivity sweeps and UI datagrid rendering.
- **Concurrent mDNS Service Discovery (`MdnsProbe.cs`)**: Replaced `List<string>` with `ConcurrentBag<string>` in `MdnsData`, ensuring thread-safe addition during UDP multicast parsing and snapshot-isolated enumeration during `Services.Distinct()` evaluation.
- **Defensive UI Thread CSV Snapshotting (`ScannerPage.cs`, `CsvExportService.cs`)**: Cloned `_scanResults` into a local list snapshot on the UI thread prior to dispatching background file export workers, preventing collection mutation races during live subnet discovery sweeps.
- **Port Scanner Progress Scale and Cancellation Re-entrancy (`PortScansPage.cs`, `ScannerPage.cs`, `TraceroutePage.cs`)**: Standardized progress bar scaling to normalized 0–100% ranges on `PortScansPage`. Centralized scan state teardown into `finally` blocks, captured local token references and deferred `_isScanning = false;` resets to prevent task overwrites during rapid start/stop sequences.
- **Device Detail View Mutation Isolation (`InventoryPage.cs`)**: Captured local target node instances in asynchronous ping, wake and port scan handlers, gating UI updates and logging behind reference equality to prevent detail pane cross-device state pollution.
- **UTC Timestamp Standardization and Lifecycle Teardown (`IntrusionDetector.cs`, `DarkPurpleTheme.cs`)**: Aligned intrusion alert records to `DateTime.UtcNow` for consistent B-tree indexing and TTL pruning. Removed premature `CancellationTokenSource.Dispose()` on window close and explicitly invoked `services.Db.Dispose()` to flush LiteDB write-ahead logs on shutdown.
- **Threat Score Persistence and Entity Identity Synchronization (`LocalDatabase.cs`)**: Prevented incoming vulnerability scores and threat levels from being overwritten in `MergeExistingNode` by updating existing records when fresh scan metrics exist and preserving prior assessments otherwise. Synchronized entity `Id`, `LastSeen` and `IsOnline` flags and unioned `OpenPorts` across repeated sweeps.
- **Backup WAL Journal Synchronization and Log Rotation (`DatabaseBackupService.cs`, `LocalDatabase.cs`)**: Flushed `_database.Checkpoint()` inside lock prior to `File.Copy` in `BackupDatabase`, guaranteeing consistent backup images without uncommitted WAL transactions. Added automated rotation of orphaned `noderadar-log.db` files in `RotateCorruptDatabase` and added physical compaction via `_db.Rebuild()` during storage pruning.
- **Adaptive Layout Bounding and Real-Time Dashboard Sync (`DashboardPage.cs`, `RadarCanvas.cs`, `DarkPurpleTheme.cs`)**: Constrained real-time ping traffic cards with a two-row Grid ("Auto,*"), wrapped dashboard root in ScrollViewer, enforced explicit minimum heights (`_radar.MinHeight = 360`, `radarCard.MinHeight = 440`, `pingsCard.MinHeight = 240`, `bentoGrid.MinHeight = 560`) and `MeasureOverride` on `RadarCanvas` to prevent canvas collapse inside ScrollViewer. Hooked `OnPropertyChanged` to pause/resume radar animation when hidden/visible, subscribed to `GlobalStatsUpdatedMessage` for synchronized alert badges and health gauge values, and wired navigation directly to Alerts.
- **Acrylic Glassmorphism Restoration (`DarkPurpleTheme.cs`)**: Adjusted root window grid background to semi-transparent `#070E1D` with 0.78 opacity, restoring Windows 11 Acrylic and Mica backdrop visibility.
- **Runtime Environment Accuracy (`SupportPage.cs`)**: Corrected runtime execution label from NativeAOT to CoreCLR JIT to reflect desktop runtime compilation.
- **Test Fixture Memory and Cancellation Cleanup (`LocalDatabaseTests.cs`, `SubnetScannerBenchmarkTests.cs`, `SnmpProbeTests.cs`, `PortScannerTests.cs`, `BannerGrabProbeTests.cs`)**: Discarded unused tuple return values in database merge tests, removed dead GC byte allocation tracking calculations in subnet benchmark tests, deleted dead `communityBytes` array allocations in SNMP probe tests and wrapped unmanaged `CancellationTokenSource` instances in using declarations to prevent handle leaks.

### Security
- **Symmetric DPAPI Protection Fallback (`CredentialVault.cs`)**: Caught `CryptographicException` in `ProtectData` alongside `PlatformNotSupportedException` to ensure graceful PBKDF2 and AES-GCM fallback in headless and unmapped user environments.
- **LiteDB Restore Pre-Validation and Instant Rollback (`DatabaseBackupService.cs`, `LocalDatabase.cs`)**: Validated backup database structure against a direct-mode temporary connection before touching active storage. Staged existing database files to `.pre_restore` with automatic rollback and reconnection recovery on I/O failures. Replaced interpolated connection strings with strongly typed `ConnectionString` configurations.
- **Fail-Fast Credential Fallback Protection (`CredentialVault.cs`)**: Removed ciphertext fallback swallowing during AEAD decryption errors, throwing `CryptographicException` directly when authentication tags or payload formats are invalid.
- **Strict Network Concurrency and Socket Ceilings (`ConnectivityMonitor.cs`, `SubnetScanner.cs`, `PortScansPage.cs`)**: Enforced reusable `SemaphoreSlim(32)` throttling across connectivity monitoring, subnet pre-sweep and post-sweep probe tasks and port scanning workflows. Bound port scan inputs to valid IPv4 addresses and numeric ranges (1–65535).
- **Explicit TLS Selection (`EmailService.cs`)**: Mapped SMTP ports directly to explicit TLS options (`SslOnConnect` on port 465, `StartTls` on port 587, `StartTlsWhenAvailable` as fallback), avoiding silent plaintext negotiation.
- **Input Sanitization and Injection Neutralization (`AppUtils.cs`, `CsvExportService.cs`, `SupportPage.cs`)**: Neutralized spreadsheet formula injection prefixes (`=`, `+`, `-`, `@`, `\t`, `\r`) in CSV exports and escaped pipe delimiters and HTML brackets in Markdown audit tables.
- **System Integrity Hash Shielding (`SecurityService.cs`, `DarkPurpleTheme.cs`)**: Centralized startup assembly integrity hashing with application version namespaces, isolating integrity checks within `SecurityService`.
- **Log Rotation Lifecycle Management (`Logger.cs`)**: Automated log cleanup checks on log file rotation, purging expired physical log files according to retention policies.

### Removed
- **Legacy Discovery Subsystem (`Core/Discovery/`)**: Purged dead `Core/Discovery/` directory (`ArpDiscoveryMethod`, `DiscoveryEngine`, `IDiscoveryMethod`, `MdnsDiscoveryMethod`, `NetworkDevice` and `SsdpDiscoveryMethod`) and retired obsolete discovery test fixtures.
- **Dead Dependency Purge (`NodeRadarPro.csproj`, `Directory.Packages.props`)**: Removed unused `Tmds.DBus.Protocol` package references.
- **Unused System.Management Package Reference (`NodeRadarPro.csproj`, `Directory.Packages.props`)**: Removed unused `System.Management` dependency from project configuration and central package management, reducing package restore and build footprint.
- **Dead Core Types and UI Tokens (`Messages.cs`, `SupportPage.cs`, `RadarCanvas.cs`, `ThemeTokens.cs`)**: Pruned unreferenced `NavigateToPageMessage` record, deleted orphaned `MakeChangelogItem` rendering method, excised dead `CyanContainer` color constant, pruned `SvgRefresh` vector and removed unreferenced `ColorPrimary`, `ButtonRadius` and 10 unused brush definitions (`SurfaceContainer`, `SurfaceVariant`, `SurfaceBright`, `InversePrimary`, `Secondary`, `SecondaryContainer`, `OnSecondaryContainer`, `TertiaryContainer`, `OnTertiaryContainer` and `OutlineVariant`).
- **Obsolete UI Test Accessors and Dead Settings (`InventoryGridControl.cs`, `DashboardPage.cs`, `SettingsPage.cs`, `AppSettings.cs`, `AppSettingsTests.cs`)**: Removed obsolete headless test accessors (`SelectedMac` on `InventoryGridControl`, `Radar` on `DashboardPage`, `PromiscuousToggle` on `SettingsPage`) and pruned unconsumed settings (`EnablePromiscuous`, `EnableOfflineFadeOut` and `FadeOutSeconds`) along with their serialization test assertions.

## [2.1.0] - 2026-10-14

### Added
- **Email Service Test Seam and Disposal Guard (`EmailService.cs`)**: Added mockable `ISmtpClient` dependency injection seam while ensuring injected client overrides are not prematurely disposed.
- **Comprehensive Subsystem and Probe Test Expansion (`tests/NodeRadarPro.Tests/`)**: Added 119 unit tests across models (`UptimeSnapshotTests.cs`, `AlertEventTests.cs`, `NetworkDeviceTests.cs`, `FingerprintResultTests.cs`, `LogEntryTests.cs`), fingerprinting probes (`MacOuiProbeTests.cs`, `NbnsProbeTests.cs`, `MdnsProbeTests.cs`, `SsdpProbeTests.cs`, `SnmpProbeTests.cs`, `BannerGrabProbeTests.cs`), diagnostics (`AppUtilsTests.cs`, `LoggerTests.cs`, `EmailServiceTests.cs`) and UI controls (`RadarCanvasTests.cs`), bringing total passing test count to 539 tests.
- **UI Test Headless Lifecycle Synchronization (`tests/NodeRadarPro.Tests/UI/AvaloniaTestApp.cs`, `StatCardAlignmentTests.cs`, `RadarCanvasTests.cs`, `UptimeChartControlTests.cs`)**: Introduced thread-safe `AvaloniaTestApp.EnsureInitialized` and isolated Avalonia UI test fixtures with `[Collection("AvaloniaUIStaticState")]` to eliminate concurrent platform initialization races.

### Changed
- **Non-Blocking Asynchronous NetBIOS Discovery (`ArpResolver.cs`, `ISubnetScannerDependencies.cs`, `SubnetScanner.cs`)**: Replaced blocking threadpool execution with native non-blocking `UdpClient.ReceiveAsync` I/O with explicit timeout and cancellation token propagation.
- **HTTP Client Connection Pooling (`BannerGrabProbe.cs`, `SsdpProbe.cs`)**: Initialized static probe `HttpClient` instances with `SocketsHttpHandler` and a 15-minute `PooledConnectionLifetime`.
- **Modular Canvas Drawing Routines (`RadarCanvas.cs`)**: Decomposed the monolithic `Render` drawing method into discrete static and instance helpers (`RenderBackgroundAndGrid`, `RenderRadarSweep`, `RenderNetworkNodes` and `RenderZoomOverlay`) while preserving zero-allocation 60 FPS drawing loops.
- **Central Package Management Upgrades (`Directory.Packages.props`)**: Upgraded `Avalonia` suite to 12.1.4, `Roslynator.Analyzers` to 5.0.1, `System.Management`, `System.Windows.Extensions` and `System.Security.Cryptography.ProtectedData` to 10.0.13, `Microsoft.NET.Test.Sdk` to 18.11.0, and `xunit.v3` / `xunit.runner.visualstudio` test tooling to 4.0.2 and 4.0.1.
- **Comprehensive Analyzer Hardening and Fatal WarningsAsErrors Gate Promotion (`Directory.Build.props`, `.editorconfig`)**: Promoted 16 static analysis rules to fatal compiler errors (`CA5398`, `CA1867`, `CA1816`, `CA1835`, `CA1850`, `CA1840`, `CA1836`, `CA1860`, `CA1866`, `CA1859`, `CA1068`, `CA2263`, `S107`, `CA1307`, `CA2016` and `S1192`). Configured `S3776` (Cognitive Complexity) as an advisory rule in `.editorconfig` to avoid false-positive breaks on low-level binary packet parsers and static dictionary tables, and scoped `S1192` to none on `VendorLookup.cs` and test suites.
- **Pre-Push Git Hook Test Enforcement (`scripts/Install-GitHooks.ps1`, `.git/hooks/pre-push`)**: Updated the local pre-push git hook to execute `Test-MasterGate.ps1 -WithTests`, guaranteeing the full 539-test suite and Roslyn formatting verification pass before any code can be pushed to remote branches.
- **Cryptographic Protocols and Stream Buffer Optimization (`BannerGrabProbe.cs`, `BannerGrabProbeTests.cs`)**: Replaced hardcoded `SslProtocols` with `SslProtocols.None` (`CA5398`) to allow OS TLS version negotiation. Converted stream read and write calls to `Memory<byte>` overloads (`CA1835`).
- **Memory Allocation and API Primitives Hardening (`CredentialVault.cs`, `Logger.cs`, `SecurityServiceTests.cs`, `LogEntryTests.cs`, `SideNavBar.cs`, `MdnsDiscoveryMethod.cs`, `MdnsProbe.cs`)**: Swapped `SHA256.Create()` with zero-allocation `SHA256.HashData` (`CA1850`); replaced `Thread.CurrentThread.ManagedThreadId` with `Environment.CurrentManagedThreadId` (`CA1840`); utilized char-based string checking methods (`CA1867`, `CA1866`); and updated `Enum.IsDefined` to generic type inference (`CA2263`).
- **Collection Queries and Concrete Return Typing (`ConnectivityMonitor.cs`, `DarkPurpleTheme.cs`, `ScannerDiagnostics.cs`, `SettingsPage.cs`, `AppUtils.cs`, `ChangelogService.cs`)**: Replaced count comparisons with `IsEmpty` on concurrent collections (`CA1836`), replaced `.Any()` with `.Count > 0` on concrete lists (`CA1860`), and converted internal helpers to concrete return types (`CA1859`).
- **CancellationToken Ordering and Dispose Pattern Adherence (`TracerouteEngine.cs`, `TraceroutePage.cs`, `SsdpDiscoveryMethod.cs`, `ArpDiscoveryMethod.cs`, `LocalDatabase.cs`, `IntrusionDetectorTests.cs`, `LocalDatabaseTests.cs`, `LoggerTests.cs`, `MdnsProbeTests.cs`, `SsdpProbeTests.cs`, `SystemLogsExportTests.cs`)**: Reordered asynchronous signatures to position optional `CancellationToken` as the trailing parameter (`CA1068`); added `GC.SuppressFinalize(this)` to all custom `IDisposable` types and test fixture disposers (`CA1816`).
- **Parameter List Refactoring and Literal Deduplication (`LocalDatabase.cs`, `SettingsPage.cs`, `InventoryPage.cs`, `DarkPurpleTheme.cs`, `LocalDatabaseTests.cs`)**: Refactored `LocalDatabase.UpdateRegistration` to consume `NetworkNode` directly, collapsing 11 parameters to 1 (`S107`); refactored `SettingsPage.MakeSliderCard` to accept compact styling flags (`S107`); and defined reusable collection and index name constants in `LocalDatabase.cs` (`S1192`).
- **Fatal StringComparison Enforcement and CA1307 Promotion (`Directory.Build.props`, `.editorconfig`)**: Promoted `CA1307` ("Specify StringComparison for clarity") to a fatal MSBuild error across all builds and editor sessions, ensuring culture-sensitive string operations cannot bypass local quality gates or CI/CD pipelines.
- **Explicit StringComparison Across Discovery and Fingerprinting (`DeviceClassifierEngine.cs`, `DiscoveryEngine.cs`, `MdnsDiscoveryMethod.cs`, `SystemLogsPage.cs`, `ScannerPage.cs`, `InventoryPage.cs`, `ChangelogService.cs`, `ArpDiscoveryMethod.cs`, `SubnetScanner.cs`, `BannerGrabProbe.cs`, `WakeOnLan.cs`, `DeviceIconMapper.cs`, `MdnsProbe.cs`, `SystemLogsExportTests.cs`)**: Refactored all string evaluation call sites across production subsystems and test suites to explicitly specify `StringComparison.Ordinal` or `StringComparison.OrdinalIgnoreCase`.
- **CancellationToken Forwarding and CA2016 Promotion (`Directory.Build.props`, `.editorconfig`, `TracerouteEngine.cs`, `ConnectivityMonitor.cs`)**: Promoted `CA2016` to a build-breaking error in `Directory.Build.props` and forwarded cancellation tokens to asynchronous DNS resolution and background tasks.
- **String Literal Deduplication and Constant Centralization (`ArpDiscoveryMethod.cs`, `DiscoveryEngine.cs`, `ArpResolver.cs`, `ConnectivityMonitor.cs`, `SubnetScanner.cs`, `DeviceClassifierEngine.cs`, `EmailService.cs`, `ScannerDiagnostics.cs`, `DatabaseBackupService.cs`, `AlertsPage.cs`, `SettingsPage.cs`, `DarkPurpleTheme.cs`, `SsdpDiscoveryMethod.cs`, `ThemeTokens.cs`)**: Replaced repeated string literals with centralized constants and reused cached `ThemeTokens.DefaultFont` instances.
- **Cognitive Complexity Reduction and Method Decomposition (`DiscoveryEngine.cs`, `SsdpDiscoveryMethod.cs`, `RadarCanvas.cs`, `ScannerPage.cs`, `DarkPurpleTheme.cs`)**: Decomposed nested packet decoders and canvas routines into modular helpers (`MergeDevice`, `TryParseSsdpResponse`, `ProcessIncomingResponsesAsync`, `DrawSingleNode`, `MakeDeviceIdentityCell`, `LoadWindowIcon`, `CreatePageHost`, `SyncGlobalStatistics`, `WireSettingsEvents`, `WireWindowLifecycle`, `WireScannerAndAlertEvents`, `WireInventoryEvents`, `WireNavigationEvents`) and introduced `ShellPages` and `ShellServices` parameter records in `DarkPurpleTheme.cs` to satisfy `S107`.

### Fixed
- **ARP Sweep Deduplication (`SubnetScanner.cs`)**: Checked IP subnet bounds before atomic `discoveredMacs.TryAdd` calls, preventing redundant device allocations during sweep passes.
- **Test Section Header Pattern Correction (`.editorconfig`)**: Corrected the test section header glob to `[tests/**.cs]`, ensuring test-scoped analyzer conventions apply properly to test suites under `tests/NodeRadarPro.Tests/`.

### Security
- **Transitive Cryptography Dependency Remediation (`Directory.Packages.props`, `NodeRadarPro.csproj`, `NodeRadarPro.Tests.csproj`)**: Pinned `System.Security.Cryptography.Pkcs` to version 10.0.13, resolving vulnerability advisories GHSA-846f-f764-76mx and GHSA-2fmv-rv2c-59x7.

## [2.0.8] - 2026-10-06

### Added
- **Configurable Retention Governance (`AppSettings.cs`, `LocalDatabase.cs`)**: Introduced persisted `LogRetentionDays` (default 14), `ResolvedAlertRetentionDays` (default 30) and `UptimeRetentionDays` (default 30) settings. Updated `PruneOldData` to dynamically consume configured limits when arguments are omitted.
- **Alert Center Granular and Bulk Deletion (`AlertsPage.cs`)**: Added inline trash deletion buttons to each alert card (`SvgTrash`), along with header-level "Clear Resolved" and "Clear All" action buttons backed by 2-step click-to-confirm latches (`Confirm Clear?` with a 3-second reset timer) and dynamic enablement.
- **System Telemetry Pruning and Purging (`SystemLogsPage.cs`)**: Added inline row deletion actions (`SvgTrash`), a "Purge > 7d" one-click maintenance action and a header "Clear Logs" button with 2-step confirmation and live count synchronization.
- **Maintenance Lifecycle Controls (`SettingsPage.cs`)**: Added interactive retention sliders for system logs and resolved alerts (1-90 days) inside the Maintenance & Lifecycle group, plus an asynchronous "Purge Expired Records" maintenance trigger with live UI feedback.
- **Official Support Link Navigation (`SupportPage.cs`)**: Connected the official website button to `https://pypiestudio.com` via `AppUtils.OpenSafeUrl` with scheme validation and background process invocation.
- **Database Deletion and Pruning Test Suite (`LocalDatabaseTests.cs`)**: Added 10 unit tests verifying individual alert deletion, bulk resolved deletion, age-based alert pruning, table truncations, single log deletion, timestamp-based log pruning and settings-backed pruning fallbacks.
- **Support Hyperlink Verification (`SupportPageAuditReportTests.cs`)**: Added unit test validating safe website URL resolution and execution boundaries.

### Changed
- **Granular and Bulk LiteDB Deletion APIs (`LocalDatabase.cs`)**: Added synchronized collection operations under thread locks with automatic WAL checkpoints: `DeleteAlert`, `DeleteResolvedAlerts`, `DeleteAlerts`, `DeleteAlertsOlderThan`, `ClearAllAlerts`, `DeleteLog`, `DeleteLogsOlderThan` and `ClearLogs`.

### Fixed
- **Release Test Reporter Syntax Hardening (`release.yml`)**: Explicitly scoped PowerShell string interpolation to prevent parser errors during GitHub Actions test summary annotations.

## [2.0.7] - 2026-10-04

### Added
- **Security and Fingerprinting Diagnostics (`IntrusionDetectorTests.cs`, `DeepFingerprintEngineTests.cs`, `LocalDatabaseTests.cs`)**: Added unit tests for `IntrusionDetector.Configure` mapping, `EventAggregator` subscription edge cases, `DeepFingerprintEngine` discovery sweeps and `LocalDatabase.GetUptimeHistory` negative and zero bounds.

### Changed
- **Subnet ARP Query Pre-allocation (`SubnetScanner.cs`)**: Pre-allocated subnet HashSet for O(1) ARP table lookups.

### Security
- **Credential Protection Hardening (`CredentialVault.cs`)**: Eliminated mock password backdoor and centralized machine-local DPAPI key storage.

## [2.0.6] - 2026-10-02

### Added
- **Connectivity Monitor Exception Handling Tests (`ConnectivityMonitorTests.cs`)**: Added unit tests verifying unroutable RFC 5737 addresses execute without throwing, cancellation tokens abort in-flight sweeps cleanly and monitor tasks stop gracefully.

### Fixed
- **ICMP Probe Exception and Cancellation Hardening (`ConnectivityMonitor.cs`)**: Replaced raw catch blocks with non-logging `PingException` and `OperationCanceledException` handlers during device reachability checks, eliminating log flooding on offline endpoints while properly propagating cancellation.
- **Background Monitor Task Cancellation Safety (`ConnectivityMonitor.cs`)**: Guarded `StartMonitoringAsync` with explicit `OperationCanceledException` handling to prevent unobserved task exceptions during background service termination.
- **Headless Release Deployment Probe Resilience (`Deploy-NodeRadar.ps1`)**: Wrapped the GitHub release existence check in `SilentlyContinue` error handling, preventing PowerShell terminating error records during automated deployments.

## [2.0.5] - 2026-09-30

### Added
- **Integrity Shield Verification Tests (`SecurityServiceTests.cs`)**: Added unit tests for `SecurityService` SHA-256 hash verification.

### Changed
- **Telemetry and Scanner Memory Optimization (`Logger.cs`, `SubnetScanner.cs`)**: Pre-sized string builders to eliminate heap allocations in log export and eliminated redundant ARP dictionary cloning in `SubnetScanner`.

### Security
- **Path and Process Argument Boundaries (`AppUtils.cs`)**: Replaced raw shell string concatenations with `ProcessStartInfo.ArgumentList` in `AppUtils.OpenFolder`.

## [2.0.4] - 2026-09-29

### Added
- **Project-Wide SVG Vector Standardization (`ThemeTokens.cs`)**: Replaced Unicode emojis across UI pages and notifications with technical SVG vector icons (`ThemeTokens.VectorIcon`), adding reusable vector constants (`SvgAlertTriangle`, `SvgInfo`, `SvgClose`, `SvgDownload`, `SvgBell`, `SvgFolder`, `SvgGlobe`, `SvgRadar`, `SvgGear`, `SvgRefresh`, `ButtonContent`).
- **Dynamic Changelog Integration (`ChangelogService.cs`, `SupportPage.cs`)**: Embedded `CHANGELOG.md` directly into the assembly manifest (`NodeRadarPro.Resources.CHANGELOG.md`) with filesystem fallback. Converted `SupportPage.cs` to dynamically render recent release badges, dates and bullet items.
- **No-Emoji Regression Gate (`NoEmojiRegressionTests.cs`)**: Added automated regression test scanning all `.cs` files across `src/NodeRadarPro` to ensure zero Unicode emojis exist in the codebase.
- **Stat Card Alignment Verification (`StatCardAlignmentTests.cs`)**: Added unit tests verifying left-alignment across stat card icon factories.
- **Dynamic Changelog Unit Tests (`ChangelogServiceTests.cs`)**: Verified release parsing, version extraction and highlight bullet formatting.

### Changed
- **Telemetry Stat Card Left-Alignment (`AlertsPage.cs`, `SystemLogsPage.cs`)**: Explicitly configured `HorizontalAlignment.Left` on SVG icons inside `MakeStatCard` and `MakeLogStatCard`, correcting centered icon drift in vertical stack panels and aligning with numerical metrics.
- **Device Inventory Filter Chip Responsive Wrapping (`InventoryPage.cs`)**: Replaced unconstrained horizontal `StackPanel` with `WrapPanel` and boundary clipping on filter chips, preventing chips from overflowing past container bounds on non-maximized viewports.
- **Central Package Management Upgrades (`Directory.Packages.props`)**: Upgraded `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent` and `Avalonia.Fonts.Inter` to 12.1.3; updated `MailKit` to 4.18.1, `Moq` to 4.21.0, `coverlet.collector` to 10.1.0 and `SonarAnalyzer.CSharp` to 10.35.0.4138.

## [2.0.3] - 2026-09-27

### Added
- **Automated Embedded Asset Tests (`AssetLoadingTests.cs`)**: Added test fixtures verifying critical embedded UI assets (`.ico`, `.png`) load and open with valid byte streams via `StandardAssetLoader`. Added regression assertion ensuring whitespace in resource hostnames is caught.
- **Master Quality Gate Smoke Step (`Test-MasterGate.ps1`)**: Added Step 2/5 application startup smoke verification executing `--smoke-test` on every local quality gate run.
- **Release Packaging Preflight Check (`Build-ReleasePackage.ps1`)**: Added Step 2/3 startup smoke verification that validates published self-contained binaries prior to Inno Setup installer compilation.
- **Coverage Ratchet**: Ratcheted coverage threshold to 55.0% and deduplicated VSTest telemetry.

### Fixed
- **Avalonia Resource URI Assembly Alignment (`DarkPurpleTheme.cs`, `SideNavBar.cs`, `SupportPage.cs`)**: Fixed desktop startup crash caused by legacy space in `avares://NodeRadar Pro/...` resource URIs following assembly migration to `NodeRadarPro`. Aligned all icon and logo URIs to `avares://NodeRadarPro/...`.
- **Defensive UI Asset Loading**: Wrapped window icon and navigation logo loading routines in `try/catch` handlers with stream disposal and warning telemetry, ensuring missing or corrupt media assets never terminate the host process.
- **Fallback Binary Verification Path (`DarkPurpleTheme.cs`)**: Corrected executable fallback probe in `VerifySystemIntegrity` from `NodeRadar Pro.dll` to `NodeRadarPro.dll`.

## [2.0.2] - 2026-09-27

### Added
- **Modularized Security Audit Export (`SupportPage.cs`)**: Refactored monolithic audit generator into modular static helpers (`BuildSecurityAuditReport`, `AppendExecutiveSummary`) with strict 12-hour AM/PM formatting (`yyyy-MM-dd hh:mm:ss tt`) and mirrored unit tests.
- **Comprehensive Database Backup Tests (`DatabaseBackupServiceTests.cs`)**: Added unit and integration tests covering backup creation, custom target paths, 7-generation retention pruning, corrupt database rotation and restore failure recovery.
- **Headless Audio Testing Seam (`AudioService.cs`)**: Added `SetSoundPlayerForTesting` test seam for deterministic test runs without OS audio hardware dependencies.
- **Inventory Uptime History Test (`LocalDatabaseTests.cs`)**: Added targeted unit test verifying MAC-filtered 24-hour uptime queries for selected inventory assets.
- **Ratcheted Coverage Gate (`coverage-threshold.txt`)**: Locked code coverage ratchet threshold to 55.0% across all 349 passing tests.

### Changed
- **Zero-Allocation Linux ARP Parsing (`ArpResolver.cs`)**: Replaced whole-file `ReadAllText` and `string.Split` with streaming `StreamReader`, `ReadOnlySpan<char>` slicing in `TryParseProcNetArpLine` and `string.Create` for MAC uppercase and hyphen substitution (73% memory allocation reduction, 3.7x execution speedup).
- **Zero-Allocation Subnet IP Parsing (`SubnetScanner.cs`)**: Replaced IP string splitting with stack spans, `CountDots` and .NET 10 `HashSet.TryGetAlternateLookup<ReadOnlySpan<char>>` (0 heap bytes allocated for IP comparisons during active discovery sweeps).
- **Event-Driven Log Queue Flushing (`Logger.cs`)**: Replaced `Thread.Sleep(10)` polling loop in `Logger.FlushForTesting()` with `ManualResetEventSlim` signaling (15x faster test queue flush latency).
- **De-nested Theme Integrity Shield (`DarkPurpleTheme.cs`)**: Extracted anonymous `Task.Run` delegate into a clean static `VerifySystemIntegrity` method.
- **De-nested Inventory Fingerprinting (`InventoryPage.cs`)**: Extracted node fingerprinting logic in `OnPingClicked` into `TryFingerprintDiscoveredNodeAsync` with clean guard clauses.
- **Clean Database History Merging (`LocalDatabase.cs`)**: Flattened 7 levels of indentation in `MergeWithHistoryBulk` by extracting `MergeExistingNode` and `MergePortBanners` helpers.

### Security
- **SMTP Certificate Validation Hardening (`EmailService.cs`)**: Removed commented-out insecure SSL/TLS certificate validation bypass callback.
- **Pull Request Security Triage**: Audited 22 open pull requests against security invariants, closing unsafe contributions that stored plaintext master keys on disk, deleted live user databases in `%USERPROFILE%\Documents` during tests or invoked `rundll32.exe`.

## [2.0.1] - 2026-09-20

### Added
- **Administrative Packaging Elevation (`installer.iss`)**: Added upfront administrative elevation (`PrivilegesRequired=admin`) in Inno Setup installer.
- **Application Icon Registration**: Registered application icon for Windows Installed Apps (`UninstallDisplayIcon`).

### Changed
- **Frictionless Installer Upgrades (`installer.iss`)**: Configured frictionless upgrades with automatic directory page bypassing (`DisableDirPage=auto`) and duplicate directory warning suppression (`DirExistsWarning=no`).
- **Support Page Update Workflows (`SupportPage.cs`)**: Switched Support Page updates to a direct 1-click link opening official GitHub releases in the default web browser.
- **Dynamic Assembly Version Telemetry**: Linked UI version telemetry dynamically to assembly metadata to eliminate version drift.

### Removed
- **Bytecode Obfuscation Tooling**: Purged bytecode obfuscation tooling to provide transparent open-source binaries.
- **Legacy Auto-Update Tooling**: Purged legacy client-side auto-update download routines, temporary script runners and Authenticode certificate chain checks.
- **Unused Startup Update Configurations**: Removed unused startup update checks and ghost settings from application configuration.

## [2.0.0] - 2026-09-19

### Added
- **Open-Source Transition & Licensing (`LICENSE`, `CODE_OF_CONDUCT.md`)**: Transitioned NodeRadar Pro to open-source software under the permissive MIT License (Copyright 2026 PyPie Studio). Adopted the Contributor Covenant v2.1 Code of Conduct.
- **Automated Network Pre-Flight Diagnostics (`scripts/Test-NetworkEnvironment.ps1`)**: Added script checking Win32 `SendARP` API binding, user-mode ICMP echo sockets, network adapter precedence (physical LAN vs virtual/VPN adapters), Windows Defender Firewall UDP multicast rules (`5353` and `1900`) and storage permissions.
- **Developer Workflow Automation (`scripts/Invoke-MasterCommit.ps1`, `scripts/Get-CoverageRate.ps1`)**: Added automated commit script enforcing conventional commits, running the Master Quality Gate, synchronizing AST knowledge graphs and staging changes. Ratcheted line test coverage threshold to `52.0%` in `coverage-threshold.txt`.
- **Architecture Decision Records & Latency Benchmarks (`docs/decisions.md`, `docs/benchmarks.md`)**: Documented ADR-006 (Win32 `SendARP` Layer 2 discovery without drivers), ADR-007 (`SemaphoreSlim` socket throttling preventing `WSAENOBUFS 10055`) and ADR-008 (Heuristic multi-layer device classification). Added discovery sweep benchmarks and LiteDB transaction latency profiles.
- **Field Troubleshooting Guide (`docs/troubleshooting.md`)**: Documented resolutions for virtual adapter metric conflicts (Hyper-V, WSL2, VMware), ICMP drops vs ARP visibility on host firewalls, Wi-Fi AP isolation and VPN split-tunneling conflicts.
- **Tag-Triggered Release Pipeline (`.github/workflows/release.yml`)**: Added automated release workflow compiling Inno Setup installers, packaging portable zip archives, generating cryptographic `SHA256SUMS.txt` checksums and publishing GitHub Releases.
- **Dependabot Security Scanning (`.github/dependabot.yml`)**: Added weekly automated dependency vulnerability scanning for NuGet and GitHub Actions.

### Changed
- **Static Analysis and Analyzer Hardening (`Directory.Build.props`)**: Hardened Roslyn and Sonar analyzer rules across all projects, silenced IDE0130 folder namespace mismatches and verified 324/324 passing tests on xUnit v3.

### Removed
- **Proprietary Commercial Licensing**: Removed legacy closed-source EULA and established open development governance.

## [1.6.0] - 2026-08-30

### Added
- **Network Engine Subsystem Unit Tests (`IntrusionDetectorTests.cs`, `AlertTypeExtensionsTests.cs`, `SsdpDiscoveryMethodTests.cs`, `DeepFingerprintEngineTests.cs`)**: Added comprehensive unit test suites with constructor DI seams and isolated in-memory LiteDB fixtures.

### Changed
- **Settings View Modularization (`SettingsPage.cs`)**: Modularized monolithic `SettingsPage` constructor into dedicated private builder methods (`BuildHeaderSection`, `BuildGeneralSettingsCard`, etc.). Refactored `MakeThresholdSliderCard` into `MakeSliderCard` with unified styling parameters and eliminated card layout duplication.
- **Zero-Allocation String Evaluation (`VendorLookup.cs`)**: Resolved CA1307 string comparison warning in `VendorLookup` with zero-allocation char replacement.

### Security
- **Online Certificate Revocation Checking (`UpdateService.cs`)**: Enforced strict online certificate revocation checking (`X509RevocationMode.Online`) in `UpdateService` without insecure fallbacks.
- **Protocol Protocol Handler Hardening (`AppUtils.cs`)**: Eliminated command injection vector in `AppUtils.OpenSafeUrl` via direct `UseShellExecute = true` Windows protocol handling.
- **Cryptographic NetBIOS Transaction Identifiers (`NbnsProbe.cs`)**: Upgraded NetBIOS (NBNS) transaction ID generation to cryptographically secure `RandomNumberGenerator.Fill`.

### Removed
- **Obsolete Manufacturer Classification Routines (`VendorLookup.cs`)**: Pruned obsolete `VendorLookup.GuessDeviceType` method and synchronized OUI database code generator.

## [1.5.0] - 2026-08-29

### Changed
- **Solution Hierarchy Restructuring**: Restructured solution into standard `src/NodeRadarPro/` and `tests/NodeRadarPro.Tests/` hierarchy with 1:1 domain directory mirroring across Core, Data and UI layers.
- **Namespace Modernization**: Unified root application namespace to `NodeRadarPro` and removed MSBuild compile hacks. Modernized Inno Setup installer compilation to eliminate deprecation warnings.
- **Deterministic Script Synchronization**: Synchronized all automation scripts (`Test-MasterGate.ps1`, `Build-ReleasePackage.ps1`, `Deploy-NodeRadar.ps1`, `Test-Coverage.ps1`, `Update-OuiDatabase.ps1`).

### Removed
- **Redundant Logger Forwarding**: Excised redundant `DiagnosticLogger` forwarding wrapper in favor of direct `Logger` invocations.

## [1.0.0] - 2026-08-28

### Added
- **Initial Release of NodeRadar Pro**: Full network reconnaissance and monitoring suite including subnet scanning, device inventory, port scanning, alert notification center and system telemetry logging.
- **Central Package Management (`Directory.Packages.props`)**: Onboarded CPM across all projects.
- **xUnit v3 Test Suite**: Built initial automated test suite on xUnit v3 with 263 passing unit and integration tests.
- **Automated Deployment Pipeline (`deploy.bat`)**: Built 7-phase automated release and deployment pipeline.
- **Socket Concurrency Throttling (`PortScanner.cs`)**: Enhanced `PortScanner` with `SemaphoreSlim(64)` concurrency throttling and linked cancellation tokens.
- **Semantic Versioning Support (`UpdateService.cs`)**: Enhanced `UpdateService` with `JsonDocument` parsing and SemVer evaluation.

### Security
- **PBKDF2 and AES-GCM Key Envelope Fallback (`CredentialVault.cs`)**: Added PBKDF2 + AES-GCM encryption fallback for credentials when machine-local DPAPI is unavailable.
- **Authenticode and X.509 Chain Verification**: Added Authenticode and `X509Chain` certificate verification for update executables.
- **Integrity Self-Audit**: Hardened startup assembly integrity checking with constant-time cryptographic hash verification.
- **Command Injection Neutralization**: Fixed command injection vulnerabilities in URL opening and external process invocations.

[Unreleased]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.2.0...HEAD
[2.2.0]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.1.0...v2.2.0
[2.1.0]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.8...v2.1.0
[2.0.8]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.7...v2.0.8
[2.0.7]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.6...v2.0.7
[2.0.6]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.5...v2.0.6
[2.0.5]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.4...v2.0.5
[2.0.4]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.3...v2.0.4
[2.0.3]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.2...v2.0.3
[2.0.2]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v1.6.0...v2.0.0
[1.6.0]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v1.5.0...v1.6.0
[1.5.0]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v1.0.0...v1.5.0
[1.0.0]: https://github.com/PyPie-Studio/NodeRadar-Pro/releases/tag/v1.0.0
