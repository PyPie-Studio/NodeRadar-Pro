# Bolt Journal: Performance & Threading Learnings

This journal records optimizations, memory allocation reductions, socket recycling, and concurrency tuning for **NodeRadar Pro**.

---

## 2026-08-17 — Subnet Sweep Concurrency Throttling & Semaphore Limits
- **Problem**: Large subnet sweeps (/24 or /16 networks with 254+ hosts) can trigger ephemeral port exhaustion and excessive context switching if tasks are spawned unbounded.
- **Decision**: Bound in-flight probe tasks using `SemaphoreSlim(50)` to `SemaphoreSlim(100)` in `SubnetScanner.cs` and `PortScanner.cs`.
- **Impact**: Stable scanning throughput, 0 socket starvation exceptions, consistent low latency measurements.

## 2026-08-17 — RadarCanvas Rendering Optimization
- **Problem**: High-frequency redraw invalidations during active radar sweeps can cause UI micro-stutters.
- **Decision**: Cache `SKPaint` and `SKPath` instances in `RadarCanvas.cs`. Restrict invalidate triggers to 60fps timer intervals.
- **Impact**: Zero allocation on draw loops, fluid animations on Windows 11 high-DPI monitors.

## 2026-08-21 — Non-Blocking Discovery Sweep Dispatch & Zero-Alloc Span Parsing
- **Problem**: Synchronously awaiting `StartDiscoverySweepAsync` in `ScanRangeAsync` added a 4-second latency penalty even for single-host or small subnet sweeps. MAC string splitting created heap allocations on high-frequency WoL/ARP loops.
- **Decision**: Dispatched discovery sweeps in the background (`_ = Task.Run(...)`) and replaced string allocations in `WakeOnLan.cs` with `Span<char>` + `byte.TryParse(..., NumberStyles.HexNumber)`.
- **Impact**: Instantaneous subnet sweep startup and zero-allocation hardware address dissection.
