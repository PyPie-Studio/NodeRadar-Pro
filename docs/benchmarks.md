# NodeRadar Pro Benchmark Report

Empirical performance measurements across network discovery sweeps, memory utilization, LiteDB persistence and UI rendering.

---

## 1. Test Environment

| Parameter | Specification |
| :--- | :--- |
| Operating System | Windows 11 Pro (Build 26100, x64) |
| .NET Runtime | .NET 10.0.0 (net10.0-windows10.0.19041.0) |
| Hardware Profile | 8 cores / 16 threads @ 3.8 GHz, 32 GB RAM |
| Network Interface | 2.5 GbE Realtek RTL8125 PCIe Controller |
| Target Network | Class C `/24` subnet (254 host addresses, 18 active hosts) |

---

## 2. Discovery Sweep Throughput

Measurements taken across 5 consecutive runs on a live 254-host subnet.

| Discovery Mode | Concurrency Limit | Avg Duration | Min Duration | Max Duration | Ephemeral Sockets Used |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Win32 `SendARP` Sweep | 64 workers | 340 ms | 280 ms | 410 ms | 0 (Kernel IP Helper API) |
| ICMP Ping Sweep | 32 workers | 1,420 ms | 1,210 ms | 1,650 ms | 32 concurrent ICMP handles |
| Stealth TCP SYN Sweep (Top 5 Ports) | 64 workers | 890 ms | 760 ms | 1,040 ms | Max 64 concurrent sockets |
| Composite Deep Fingerprint Sweep | Dynamic pipeline | 3,850 ms | 3,200 ms | 4,400 ms | Capped at 64 sockets |

> [!NOTE]
> `SendARP` bypasses TCP/IP socket stack allocation entirely, resulting in microsecond response times and zero ephemeral port exhaustion.

---

## 3. Port Scanning Throughput

Benchmarked against 18 active hosts with variable latency profiles (0.4 ms LAN to 45 ms WLAN hops).

| Scan Scope | Port Count | Workers | Socket Timeout | Wall-Clock Time | Success Rate |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Standard Service Audit | 20 ports / host | 64 | 250 ms | 3.1 s | 100% |
| Extended Infrastructure Audit | 100 ports / host | 64 | 200 ms | 8.4 s | 100% |
| Custom Database Cluster Sweep | 8 ports / host | 32 | 300 ms | 1.8 s | 100% |

Dead host short-circuit: unreachable hosts evaluate `Task.WhenAny` alongside a 200 ms timeout window, terminating dead branches concurrently rather than sequentially.

---

## 4. Memory Footprint & Resource Utilization

Monitored using .NET `Process.WorkingSet64` and dotnet-counters during a continuous 1-hour discovery cycle.

| Application Lifecycle State | Private Working Set | Managed Heap Size | Active Socket Count | GC Collections (Gen 0 / 1 / 2) |
| :--- | :--- | :--- | :--- | :--- |
| Application Idle (Post-Launch) | 72 MB | 28 MB | 0 | 0 / 0 / 0 |
| Active Deep Sweep (/24) | 124 MB | 54 MB | 64 (Peak) | 4 / 1 / 0 |
| Post-Sweep Steady State | 81 MB | 31 MB | 0 | 1 / 1 / 0 |
| Continuous Sentinel Monitor (1h) | 88 MB | 36 MB | 2-4 | 12 / 3 / 0 |

---

## 5. UI Render Latency (Avalonia & SkiaSharp)

Render metrics measured using SkiaSharp microsecond timing hooks at 144 Hz display refresh.

| UI Component | Metric | Target Budget | Measured Latency | Frame Drop Rate |
| :--- | :--- | :--- | :--- | :--- |
| `RadarCanvas` 360° Sweep | Per-frame render | < 6.94 ms (144 Hz) | 1.85 ms | 0.0% |
| `UptimeChartControl` Refresh | Canvas repaint | < 6.94 ms (144 Hz) | 0.92 ms | 0.0% |
| DataGrid Sort (500 Hosts) | UI thread dispatch | < 16.0 ms | 4.30 ms | 0.0% |

Pens, brushes and formatted text layouts are pre-cached in static memory. Zero heap allocations occur during active canvas redraw passes.

---

## 6. Persistence & Storage (Embedded LiteDB v5)

All transactions executed against AES-256 encrypted LiteDB document store with thread synchronization locks.

| Operation | Batch Size | Wall-Clock Time | Notes |
| :--- | :--- | :--- | :--- |
| Batch Node Insert | 500 records | 14.2 ms | Single transaction boundary |
| Indexed Node Lookup by MAC | 1 record | 0.18 ms | B-Tree index lookup |
| Telemetry Log Prune (TTL) | 10,000 records | 38.5 ms | Background worker task |
| Database Snapshot Backup | 25 MB database | 110 ms | Rolling snapshot to `.bak` |
