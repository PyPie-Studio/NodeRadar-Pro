# Graph Report - NodeRadar Pro  (2026-08-21)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 776 nodes · 1337 edges · 40 communities (38 shown, 2 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 45 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `0a8a25cc`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- InventoryPage
- ThemeTokens
- Border
- NetworkNode
- NodeRadarPro.Core.Fingerprinting.Probes
- .Classify
- SubnetScanner
- NodeRadarPro.Core
- ScannerPage
- DhcpFingerprintProbe
- TraceroutePage
- .BuildMainWindow
- command
- RadarCanvas
- MdnsProbe
- LocalDatabase
- .RunDiscoveryAsync
- MndpProbe
- NodeRadar Pro
- SsdpProbe
- EventAggregator
- .DiscoverAsync
- OuiDatabase
- App
- .DiscoverAsync
- AlertsPage
- spawn_subagent
- .DiscoverAsync
- UptimeSnapshot
- .Refresh
- .DiscoverAsync
- LogEntry
- memory
- AlertEvent
- find_repo_root
- find_repo_root
- NodeRadarPro.Core.Discovery

## God Nodes (most connected - your core abstractions)
1. `InventoryPage` - 51 edges
2. `LocalDatabase` - 36 edges
3. `NetworkNode` - 34 edges
4. `NodeRadarPro.Core` - 33 edges
5. `ScannerPage` - 32 edges
6. `SettingsPage` - 24 edges
7. `ThemeTokens` - 20 edges
8. `RadarCanvas` - 19 edges
9. `SystemLogsPage` - 18 edges
10. `TraceroutePage` - 17 edges

## Surprising Connections (you probably didn't know these)
- `InventoryPage` --references--> `ConnectivityMonitor`  [EXTRACTED]
  UI/InventoryPage.cs → Core/ConnectivityMonitor.cs
- `InventoryPage` --references--> `NetworkNode`  [EXTRACTED]
  UI/InventoryPage.cs → Core/NetworkNode.cs
- `InventoryPage` --references--> `LocalDatabase`  [EXTRACTED]
  UI/InventoryPage.cs → Data/LocalDatabase.cs
- `SystemLogsPage` --references--> `LogLevel`  [EXTRACTED]
  UI/SystemLogsPage.cs → Core/LogEntry.cs
- `SystemLogsPage` --references--> `LocalDatabase`  [EXTRACTED]
  UI/SystemLogsPage.cs → Data/LocalDatabase.cs

## Import Cycles
- None detected.

## Communities (40 total, 2 thin omitted)

### Community 0 - "InventoryPage"
Cohesion: 0.06
Nodes (22): Control, Task, Grid, HashSet, Pen, bool, Button, CheckBox (+14 more)

### Community 1 - "ThemeTokens"
Cohesion: 0.06
Nodes (25): Action, Task, UpdateService, downloadUrl, hasUpdate, IBrush, TextBlock, DispatcherTimer (+17 more)

### Community 2 - "Border"
Cohesion: 0.07
Nodes (27): Border, ComboBox, NumericUpDown, Slider, bool, Button, CancellationTokenSource, DateTime (+19 more)

### Community 3 - "NetworkNode"
Cohesion: 0.06
Nodes (25): AppSettings, AudioService, bool, CancellationToken, ConcurrentDictionary, List, Task, ConnectivityMonitor (+17 more)

### Community 4 - "NodeRadarPro.Core.Fingerprinting.Probes"
Cohesion: 0.07
Nodes (25): CancellationToken, HttpClient, NetworkNode, ProbeResult, Task, BannerGrabProbe, MacOuiProbe, CancellationToken (+17 more)

### Community 5 - ".Classify"
Cohesion: 0.06
Nodes (24): CancellationToken, Lazy, List, NetworkNode, Task, DeepFingerprintEngine, List, NetworkNode (+16 more)

### Community 6 - "SubnetScanner"
Cohesion: 0.10
Nodes (17): DateTime, DllImport, List, Mac, object, ArpResolver, Task, ScannerDiagnostics (+9 more)

### Community 7 - "NodeRadarPro.Core"
Cohesion: 0.07
Nodes (17): CancellationToken, NetworkNode, ProbeResult, Task, NetworkNode, GlobalStatsUpdatedMessage, NavigateToPageMessage, NodesUpdatedMessage (+9 more)

### Community 8 - "ScannerPage"
Cohesion: 0.08
Nodes (19): Expander, isNew, node, bool, Button, CancellationTokenSource, CheckBox, ConcurrentDictionary (+11 more)

### Community 9 - "DhcpFingerprintProbe"
Cohesion: 0.10
Nodes (16): CancellationToken, ConcurrentDictionary, Dictionary, NetworkNode, ProbeResult, Task, DhcpData, DhcpFingerprintProbe (+8 more)

### Community 10 - "TraceroutePage"
Cohesion: 0.12
Nodes (15): CancellationToken, IPAddress, Task, RouteHop, TracerouteEngine, bool, Button, CancellationTokenSource (+7 more)

### Community 11 - ".BuildMainWindow"
Cohesion: 0.12
Nodes (12): Window, CheckBox, IBrush, RoutedEventArgs, ScrollViewer, StackPanel, string, TextBlock (+4 more)

### Community 12 - "command"
Cohesion: 0.09
Nodes (22): MEMORY_FILE_PATH, command, cwd, enabled, type, mcp, graphify, memory (+14 more)

### Community 13 - "RadarCanvas"
Cohesion: 0.11
Nodes (14): Color, Point, PointerEventArgs, PointerPressedEventArgs, PointerReleasedEventArgs, PointerWheelEventArgs, bool, DispatcherTimer (+6 more)

### Community 14 - "MdnsProbe"
Cohesion: 0.18
Nodes (9): CancellationToken, ConcurrentDictionary, List, NetworkNode, ProbeResult, Task, MdnsData, MdnsProbe (+1 more)

### Community 15 - "LocalDatabase"
Cohesion: 0.17
Nodes (5): IEnumerable, Lazy, LiteDatabase, string, LocalDatabase

### Community 16 - ".RunDiscoveryAsync"
Cohesion: 0.29
Nodes (6): Action, CancellationToken, List, Task, DiscoveryEngine, NetworkDevice

### Community 17 - "MndpProbe"
Cohesion: 0.20
Nodes (9): CancellationToken, ConcurrentDictionary, NetworkNode, ProbeResult, Task, MndpData, MndpProbe, MndpData (+1 more)

### Community 18 - "NodeRadar Pro"
Cohesion: 0.13
Nodes (14): net10.0-windows10.0.19041.0, NodeRadar Pro, Avalonia (12.0.2), Avalonia.Desktop (12.0.2), Avalonia.Fonts.Inter (12.0.2), Avalonia.Themes.Fluent (12.0.2), AvaloniaUI.DiagnosticsSupport (2.2.1), LiteDB (5.0.21) (+6 more)

### Community 19 - "SsdpProbe"
Cohesion: 0.20
Nodes (9): CancellationToken, ConcurrentDictionary, HttpClient, NetworkNode, ProbeResult, Task, SsdpData, SsdpProbe (+1 more)

### Community 20 - "EventAggregator"
Cohesion: 0.15
Nodes (9): Action, ConcurrentDictionary, Lazy, EventAggregator, WeakSubscription, Delegate, MethodInfo, Type (+1 more)

### Community 21 - ".DiscoverAsync"
Cohesion: 0.21
Nodes (6): Action, CancellationToken, IPAddress, List, Task, MdnsDiscoveryMethod

### Community 22 - "OuiDatabase"
Cohesion: 0.17
Nodes (8): IEnumerable, Lazy, LiteDatabase, ObjectId, OuiDatabase, OuiEntry, IDisposable, ILiteCollection

### Community 23 - "App"
Cohesion: 0.18
Nodes (6): App, AppBuilder, Application, NodeRadar_Pro, Program, STAThread

### Community 24 - ".DiscoverAsync"
Cohesion: 0.21
Nodes (8): Action, CancellationToken, Dictionary, DllImport, IPAddress, List, Task, ArpDiscoveryMethod

### Community 25 - "AlertsPage"
Cohesion: 0.27
Nodes (5): ObjectId, DateTime, StackPanel, string, AlertsPage

### Community 26 - "spawn_subagent"
Cohesion: 0.36
Nodes (8): find_repo_root(), load_skill_instructions(), main(), Path, Traverse upwards to find the repository root (containing .agents/)., Load skill instructions from SKILL.md file., spawn_subagent(), Any

### Community 27 - ".DiscoverAsync"
Cohesion: 0.25
Nodes (6): Action, CancellationToken, IPAddress, List, Task, SsdpDiscoveryMethod

### Community 28 - "UptimeSnapshot"
Cohesion: 0.25
Nodes (4): DateTime, ObjectId, UptimeSnapshot, List

### Community 30 - ".DiscoverAsync"
Cohesion: 0.25
Nodes (6): Action, CancellationToken, IPAddress, List, Task, IDiscoveryMethod

### Community 31 - "LogEntry"
Cohesion: 0.38
Nodes (4): DateTime, ObjectId, LogEntry, LogLevel

### Community 32 - "memory"
Cohesion: 0.29
Nodes (6): MEMORY_FILE_PATH, npx, python, graphify, memory, @modelcontextprotocol/server-memory

### Community 33 - "AlertEvent"
Cohesion: 0.40
Nodes (4): DateTime, ObjectId, AlertEvent, AlertType

### Community 34 - "find_repo_root"
Cohesion: 0.67
Nodes (3): find_repo_root(), main(), Path

### Community 35 - "find_repo_root"
Cohesion: 0.67
Nodes (3): find_repo_root(), main(), Path

## Knowledge Gaps
- **38 isolated node(s):** `MndpData`, `SsdpData`, `GlobalStatsUpdatedMessage`, `NavigateToPageMessage`, `DhcpData` (+33 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `NodeRadarPro.Core` connect `NodeRadarPro.Core` to `AlertEvent`, `ThemeTokens`, `NetworkNode`, `NodeRadarPro.Core.Fingerprinting.Probes`, `.Classify`, `SubnetScanner`, `NodeRadarPro.Core.Discovery`, `TraceroutePage`, `UptimeSnapshot`, `LogEntry`?**
  _High betweenness centrality (0.175) - this node is a cross-community bridge._
- **Why does `NetworkNode` connect `NetworkNode` to `InventoryPage`, `ThemeTokens`, `SubnetScanner`, `ScannerPage`, `RadarCanvas`, `UptimeSnapshot`?**
  _High betweenness centrality (0.117) - this node is a cross-community bridge._
- **Why does `InventoryPage` connect `InventoryPage` to `ThemeTokens`, `Border`, `NetworkNode`, `NodeRadarPro.Core`, `LocalDatabase`?**
  _High betweenness centrality (0.115) - this node is a cross-community bridge._
- **What connects `MndpData`, `SsdpData`, `GlobalStatsUpdatedMessage` to the rest of the system?**
  _38 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `InventoryPage` be split into smaller, more focused modules?**
  _Cohesion score 0.06349206349206349 - nodes in this community are weakly interconnected._
- **Should `ThemeTokens` be split into smaller, more focused modules?**
  _Cohesion score 0.057912457912457915 - nodes in this community are weakly interconnected._
- **Should `Border` be split into smaller, more focused modules?**
  _Cohesion score 0.06567992599444958 - nodes in this community are weakly interconnected._