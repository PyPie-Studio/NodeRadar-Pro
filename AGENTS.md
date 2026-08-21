# Agent Guide: NodeRadar Pro

Welcome to the **NodeRadar Pro** repository. This project is an enterprise-grade network observability suite, asset discovery engine, and security sentinel built with Avalonia UI (.NET 10), embedded LiteDB, and low-level networking primitives.

---

## ⚡ Skills, Rules & MCP Capabilities

This solution uses a comprehensive AI agent customization framework with auto-discovered skills, rules, and MCP integrations:

- **14 Workspace Skills:** Auto-discovered from [`.agents/skills/`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/skills). See [`SKILLS.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/SKILLS.md) for the full registry.
- **10 Enforcement Rules:** Auto-discovered from [`.agents/rules/`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules):
  - [`ui-ux-standards.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/ui-ux-standards.md) — Dark-Purple Glassmorphism, Radar canvas rendering, 12-hour AM/PM time, 100% English LTR
  - [`performance-guardrails.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/performance-guardrails.md) — Socket recycling, SemaphoreSlim throttling, zero-allocation packet parsing, Canvas frame rate
  - [`security-guardrails.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/security-guardrails.md) — DPAPI encryption, LiteDB AES-256, path sanitization, constant-time comparison, SHA-256 self-audit
  - [`dead-code-hygiene.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/dead-code-hygiene.md) — 17 hard Roslyn/Sonar analyzer rules
  - [`pr-review-checklist.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/pr-review-checklist.md) — 8-point mandatory PR audit checklist + network concurrency gotchas
  - [`superpowers.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/superpowers.md) — Brainstorm $\rightarrow$ Plan Gate $\rightarrow$ Approval $\rightarrow$ TDD/Execution $\rightarrow$ Review
  - [`git-commit-conventions.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/git-commit-conventions.md) — Conventional commit format
  - [`planning-discipline.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/planning-discipline.md) — Plan and approval discipline
  - [`push-and-pr-gate.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/push-and-pr-gate.md) — Master quality gate verification
  - [`graphify.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/graphify.md) — Codebase knowledge graph queries
- **MCP Servers:** Configured in [`opencode.json`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/opencode.json) and [`mcp.json`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/mcp.json):
  - `memory`: Cross-session knowledge graph stored in `.agents/memory/memory.json`.
  - `graphify`: Codebase AST knowledge graph query server.
- **Change Journaling (`.jules/`):** Mandatory 4-pillar learning logs:
  - `.jules/bolt.md` (Performance & Threading)
  - `.jules/palette.md` (UI/UX & Avalonia Glassmorphism)
  - `.jules/sentinel.md` (Security, DPAPI, Encryption, Tamper Detection)
  - `.jules/sweeper.md` (Code Health, Socket & Token Lifecycle)

---

## 🏗 Architecture & Tech Stack

NodeRadar Pro is engineered as a standalone, hardened, high-performance desktop application:

- **Target Framework:** `.NET 10.0 (net10.0-windows10.0.19041.0)`
- **UI Framework:** Avalonia UI 12.0.2 with Fluent Theme, custom `ThemeTokens.cs`, `DarkPurpleTheme.cs`, SkiaSharp canvas rendering (`RadarCanvas.cs`), and dynamic charts (`UptimeChartControl.cs`).
- **Core Network Engines ([`Core/`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/Core)):**
  - `SubnetScanner.cs` / `ArpResolver.cs`: High-speed subnet sweeping via Win32 `SendARP` API, multi-threaded ICMP ping probing, and TCP syn probes.
  - `PortScanner.cs`: Service port discovery for target vectors (SSH, RDP, SMB, HTTP/S, Databases).
  - `IntrusionDetector.cs` / `IntrusionAlerter.cs`: Real-time background sentinel detecting unauthorized MAC addresses joining the subnet.
  - `ConnectivityMonitor.cs` / `TracerouteEngine.cs`: Real-time latency tracking, jitter analysis, sliding-window uptime snapshots, and multi-hop ICMP traceroute.
  - `Discovery/` & `Fingerprinting/`: Heuristic OS identification (MAC OUI lookup, open port signatures, mDNS, SSDP, DHCP, WS-Discovery per [`NetworkAnalysis.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/NetworkAnalysis.md)).
- **Persistence & Security ([`Data/`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/Data)):**
  - `LocalDatabase.cs`: Embedded **LiteDB v5** with AES-256 encryption.
  - `System.Security.Cryptography.ProtectedData` (Windows DPAPI): Machine-local secure password and encryption key storage.
  - SHA-256 self-audit integrity shield on startup.
- **Packaging & Protection:**
  - `Obfuscar` (IL obfuscation).
  - Inno Setup 6 packaging (`Inno/installer.iss`) supporting both All-Users (Admin) and Current-User (Non-Admin) installations.

---

## 🚀 Essential Commands

### Build & Run
```powershell
# Build the solution in Debug
dotnet build NodeRadarPro.slnx

# Build in Release with Warnings as Errors
dotnet build NodeRadarPro.slnx -c Release

# Run the Desktop Application
dotnet run --project "NodeRadar Pro.csproj"

# Re-format code according to .editorconfig rules
dotnet format NodeRadarPro.slnx
```

### Local CI Master Gate
Verification is mandatory before committing or pushing changes:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1
```
The master gate validates:
1. Release build with zero compiler warnings (`TreatWarningsAsErrors=true`).
2. Roslynator & SonarAnalyzer static analysis rules.
3. Code formatting verification (`dotnet format --verify-no-changes`).
4. NuGet vulnerability check.

### Release Packaging
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-ReleasePackage.ps1
```

---

## 🛠 Development Patterns & Conventions

### 1. Concurrency & Socket Lifecycle
- **SemaphoreSlim Throttling:** Always limit concurrent socket connections and ping requests (e.g. 50-100 max concurrent tasks) to avoid ephemeral port exhaustion.
- **CancellationToken Propagation:** Every asynchronous network operation must accept and observe a `CancellationToken` to enable instantaneous scan aborts without thread hangs.
- **Resource Disposal:** Wrap all `Socket`, `TcpClient`, `Ping`, and `CancellationTokenSource` instances in `using` declarations or structured `Dispose()` calls.

### 2. UI/UX Standards
- **Dark-Purple Glassmorphic Theme:** Base `#0D0B14`, accent `#6B21A8`, highlight `#A855F7`, surface `#1F1A2E`.
- **12-Hour AM/PM Time Format Standard:**
  - Format: `yyyy-MM-dd hh:mm:ss tt` / `hh:mm:ss tt`.
  - **NEVER** use 24-hour military time (`HH:mm` or `HH:mm:ss`).
- **Strict English LTR:** All UI views, status indicators, export logs, and tooltips are strictly in English with `FlowDirection="LeftToRight"`.

### 3. Change Journaling (`.jules/`)
All non-trivial changes must be documented in the corresponding journal:
- `.jules/bolt.md`: Performance, memory allocations, scan throughput, buffer pooling.
- `.jules/palette.md`: Avalonia UI/UX design, visual feedback, theme styling.
- `.jules/sentinel.md`: Security, DPAPI, LiteDB encryption, input sanitization, integrity checks.
- `.jules/sweeper.md`: Code hygiene, refactoring, thread cleanup, socket disposal.

---

## ⚠️ Important Gotchas

1. **Win32 `SendARP` vs Non-Admin:** The `SendARP` API works without admin privileges, but raw ICMP/PCAP packet sniffing may require elevated rights or fallback to standard socket probes.
2. **LiteDB Thread-Safety:** LiteDB instances must be accessed synchronously via thread-safe patterns or a single shared instance; do not open concurrent read/write handles across threads.
3. **Avalonia UI Thread Dispatching:** Never update UI observable properties directly from background scan worker threads. Always use `Dispatcher.UIThread.Post(...)` or `InvokeAsync(...)`.
4. **Obfuscar Name Renaming:** Entry point (`NodeRadar_Pro.Program`) and reflection-bound models must be excluded from obfuscation in `obfuscar.xml`.
