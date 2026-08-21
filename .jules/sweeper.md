# Sweeper Journal: Architecture & Code Health Learnings

This journal records refactoring, socket and token lifecycle hygiene, and dead code elimination for **NodeRadar Pro**.

---

## 2026-08-17 — CancellationToken Propagation Across Asynchronous Engines
- **Problem**: Subnet sweeps and traceroute engines without strict token propagation cannot be aborted immediately when the user clicks "Stop Scan".
- **Decision**: Propagate `CancellationToken` through all asynchronous loops in `SubnetScanner.cs`, `PortScanner.cs`, and `TracerouteEngine.cs`.
- **Impact**: Instantaneous scan cancellation with zero orphaned background socket threads.

## 2026-08-17 — Static Analysis & Roslynator/SonarAnalyzer Baseline
- **Problem**: Codebase accumulation of unused local variables, empty catch blocks, and missing type constraints.
- **Decision**: Onboard `Directory.Build.props` with `SonarAnalyzer.CSharp` and `Roslynator.Analyzers`, backed by `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- **Impact**: Zero warning tolerance enforced by compiler on every build.
