---
trigger: always_on
description: Mandatory 8-point PR review checklist - business utility, network safety, socket lifecycle, LiteDB integrity, path security, UI/UX consistency, quality gate, and journaling.
alwaysApply: true
---
# PR Review Checklist

1. **Business Utility**: Verify code directly implements network scanning, device identification, security alerting, or UI observability requirements without dead wrappers or speculative abstractions.
2. **Network Concurrency & Socket Lifecycle**:
   - Verify every network call has appropriate timeout and cancellation token propagation.
   - Ensure all `Socket`, `TcpClient`, and `Ping` instances are bounded by `SemaphoreSlim` and properly disposed.
   - Verify zero socket exhaustion / ephemeral port leaks during 254-host subnet sweeps.
3. **Thread Safety & UI Dispatching**:
   - Background scans MUST NEVER manipulate Avalonia UI controls directly.
   - Verify thread transitions use `Dispatcher.UIThread.Post(...)` or `InvokeAsync(...)`.
4. **Security & Cryptography**:
   - Verify credentials / sensitive settings use DPAPI (`ProtectedData`).
   - Verify LiteDB database connections are protected by AES-256 encryption.
   - Verify file export and updater paths use `Path.GetFullPath()` boundary validation.
   - Verify constant-time validation (`CryptographicOperations.FixedTimeEquals`) for checksums.
5. **UI/UX Consistency**:
   - Verify 12-hour AM/PM time formatting standard (`yyyy-MM-dd hh:mm:ss tt`).
   - Verify 100% English LTR layout across all controls, headers, and logs.
   - Verify Dark-Purple glassmorphic theme compliance.
6. **Empirical Gate Verification**:
   - `scripts/Test-MasterGate.ps1` MUST exit 0 with zero build warnings and zero format violations before any commit or merge.
7. **No Residual Scratch Files**:
   - Verify no temporary test scripts, `.orig`, `.bak`, `.tmp`, or scratch `.csproj` files leak into the repository.
8. **Mandatory Journaling**:
   - Non-trivial modifications MUST be journaled in the corresponding `.jules/*.md` file (`bolt.md`, `palette.md`, `sentinel.md`, `sweeper.md`).
