## What this PR does
<!-- A concise summary of the proposed feature, optimization or bugfix -->

## Files changed
<!-- List each file modified, added or removed and briefly explain why -->

## How I tested it
<!-- Describe your test environment (e.g. Windows 11 24H2, Windows 10 22H2), network topology and test results -->

## Quality & Architectural Checklist
All PRs must adhere to the engineering standards outlined in `CONTRIBUTING.md`:

- [ ] **Master Quality Gate Passed**: Executed locally and passed cleanly:
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1 -WithTests
  ```
- [ ] **Zero Warnings**: Solution compiles under `TreatWarningsAsErrors=true` with zero compiler, Roslynator or SonarAnalyzer warnings.
- [ ] **Socket Throttling**: Multi-target sweeps or probes utilize `SemaphoreSlim` to prevent ephemeral socket exhaustion (`WSAENOBUFS 10055`).
- [ ] **Cancellation Resilient**: Asynchronous network operations accept and observe a `CancellationToken`.
- [ ] **Structured Disposal**: Sockets, clients, ping engines and tokens are wrapped in `using` scopes or explicit disposal.
- [ ] **12-Hour AM/PM Standard**: Any user-facing time representations follow 12-hour AM/PM format (`yyyy-MM-dd hh:mm:ss tt` or `hh:mm:ss tt`).
- [ ] **Local-Only Mandate**: Zero telemetry phone-home, zero tracking beacons and zero external cloud service calls.
- [ ] **No Leakage**: No database files (`*.db`), temporary logs, scratch files or personal credentials committed.
