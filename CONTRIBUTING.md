# Contributing to NodeRadar Pro

Thank you for your interest in contributing to **NodeRadar Pro**.

NodeRadar Pro is an open-source network observation and diagnostics toolkit built with C# and Avalonia UI on .NET 10. To keep the codebase clean, performant and reliable across diverse network environments, all contributions must adhere to the engineering standards outlined below.

---

## Core Engineering Invariants

Every Pull Request must respect these architectural requirements:

1. **Local-Only Mandate**: NodeRadar Pro is strictly local-first. PRs introducing external analytics, third-party cloud telemetry, tracking beacons or paid cloud API calls will be rejected.
2. **Socket Throttling & Leak Prevention**: All multi-host sweeps and multi-port probes must use `SemaphoreSlim` concurrency throttling (32–64 max concurrent probes) to prevent ephemeral port exhaustion (`WSAENOBUFS 10055`).
3. **CancellationToken Propagation**: All asynchronous network operations must accept and honor `CancellationToken` for responsive cancellation.
4. **Structured Disposal**: All `Socket`, `TcpClient`, `Ping` and `CancellationTokenSource` instances must be wrapped in `using` declarations or disposed explicitly.
5. **Background Thread Offloading**: Network sweeps, port scans, ARP queries and database operations must run on background threads (`Task.Run`). Only dispatch to the UI thread via `Dispatcher.UIThread.Post` or `InvokeAsync` when updating observable collections.
6. **12-Hour AM/PM Time Format Standard**: UI time representations in DataGrids, uptime charts, telemetry cards and logs must follow 12-hour AM/PM formatting (`yyyy-MM-dd hh:mm:ss tt` or `hh:mm:ss tt`). 24-hour military time is prohibited in user-facing UI.
7. **No Kernel Drivers**: Discovery must rely purely on user-mode Win32 APIs (`SendARP`) and standard TCP/UDP/ICMP sockets without requiring WinPcap or Npcap installations.

---

## Local Development & Testing Workflow

### 1. Prerequisites

- **Operating System**: Windows 10 (Build 19041+) or Windows 11
- **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or latest net10 preview/release)
- **PowerShell**: PowerShell 5.1+ or PowerShell 7+

### 2. Building the Solution

Clone the repository and compile in Release mode:

```powershell
git clone https://github.com/PyPie-Studio/NodeRadar-Pro.git
cd NodeRadar-Pro
dotnet build NodeRadarPro.slnx --configuration Release
```

### 3. Running Unit Tests

Run the xUnit v3 test suite:

```powershell
dotnet test NodeRadarPro.slnx --configuration Release --nologo
```

### 4. Master Quality Gate

Before opening a pull request or pushing commits, verify your changes against our automated Master Quality Gate:

```powershell
# Run full gate (Release build + xUnit test suite + Roslyn formatting + NuGet vulnerability check)
powershell -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1 -WithTests
```

The gate will fail if:
- Any compiler warning is generated (`TreatWarningsAsErrors=true`)
- Any unit test fails
- Code formatting does not match `dotnet format`
- Any vulnerable NuGet dependency is detected

Fix formatting violations automatically with:

```powershell
dotnet format NodeRadarPro.slnx
```

### 5. Git Pre-Push Hook (Optional)

You can automatically enforce the Master Gate on every `git push`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Install-GitHooks.ps1 -Test
```

---

## Submitting a Pull Request

1. **Fork the Repository** to your personal GitHub account.
2. **Create a Feature Branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Commit with Conventional Messages**:
   Use standard commit format: `feat(scope): description` or `fix(scope): description`.
   Examples:
   - `feat(discovery): add custom subnet CIDR mask parser`
   - `fix(scanner): release socket semaphore on probe timeout`
   - `test(engines): add unit tests for packet loss calculations`
4. **Verify Quality Gate**:
   Ensure `powershell -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1 -WithTests` passes cleanly.
5. **Open a Pull Request**:
   Fill out the PR template with a description of the change, tested network environments and test outcomes.
