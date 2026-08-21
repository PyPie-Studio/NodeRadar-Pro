---
trigger: always_on
description: Mandatory 8-point PR review checklist - business utility, network safety, socket lifecycle, LiteDB integrity, path security, UI/UX consistency, quality gate, and journaling.
alwaysApply: true
---
# PR Review Checklist

1. **Business Utility & PR Triage**:
   - Verify code directly implements network scanning, device identification, security alerting, or UI observability requirements.
   - **Triage & Supersession**: Reject/close low-signal PRs (isolated single-line comment edits, speculative constructor shredding, redundant catch logging) if superseded by cohesive architectural PRs.
2. **Network Concurrency & Socket Lifecycle**:
   - Verify every network call has appropriate timeout and cancellation token propagation.
   - Ensure all `Socket`, `TcpClient`, and `Ping` instances are bounded by `SemaphoreSlim` and properly disposed.
   - Verify zero socket exhaustion / ephemeral port leaks during 254-host subnet sweeps.
   - **Multi-Protocol Stream Encapsulation**: When negotiating TLS or application protocols (e.g. `BannerGrabProbe.cs`), ensure all reads/writes target the active negotiated `Stream` (`activeStream`), never the raw socket stream.
3. **Unit Test Socket Stability & Mock Isolation**:
   - **Dynamic Test Port Allocation**: Never hardcode occupied ports in mock listeners. Use dynamic port allocation (`new TcpListener(IPAddress.Loopback, 0)`) or fallback across safe non-TLS port ranges.
   - **Socket Disposal Race Prevention**: Mock TCP listeners must not close sockets immediately after writing before the client completes reading.
   - **Service Testability**: Network services (e.g. `UpdateService`) and databases (`LocalDatabase`) must support dependency injection (`HttpClient`, test constructor overloads).
4. **Thread Safety & UI Dispatching**:
   - Background scans MUST NEVER manipulate Avalonia UI controls directly.
   - Verify thread transitions use `Dispatcher.UIThread.Post(...)` or `InvokeAsync(...)`.
5. **Security & Cryptography**:
   - Verify credentials / sensitive settings use DPAPI (`ProtectedData`).
   - Verify LiteDB database connections are protected by AES-256 encryption.
   - Verify updater and file download URLs enforce HTTPS, exact trusted domain whitelist (`github.com`), and repository-scoped path boundaries.
   - Verify file export paths use `Path.GetFullPath()` boundary validation.
   - Verify constant-time validation (`CryptographicOperations.FixedTimeEquals`) for checksums.
6. **UI/UX Consistency**:
   - Verify 12-hour AM/PM time formatting standard (`yyyy-MM-dd hh:mm:ss tt`).
   - Verify 100% English LTR layout across all controls, headers, and logs.
   - Verify Dark-Purple glassmorphic theme compliance.
7. **Empirical Gate Verification**:
   - `scripts/Test-MasterGate.ps1` MUST exit 0 with zero build warnings and zero format violations before any commit or merge.
8. **No Residual Scratch Files**:
   - Verify no temporary test scripts, `.orig`, `.bak`, `.tmp`, or scratch `.csproj` files leak into the repository.
9. **Mandatory Journaling**:
   - Non-trivial modifications MUST be journaled in the corresponding `.jules/*.md` file (`bolt.md`, `palette.md`, `sentinel.md`, `sweeper.md`).
