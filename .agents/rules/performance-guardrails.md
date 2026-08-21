---
trigger: always_on
description: Performance guardrails - Concurrency throttling via SemaphoreSlim, socket recycling, zero-allocation buffers, CancellationToken propagation, RadarCanvas rendering optimization, and dictionary lookups.
alwaysApply: true
---
# Performance Guardrails

- **SemaphoreSlim Concurrency Throttling**: MANDATORY for all multi-node sweeps (`SubnetScanner.cs`, `PortScanner.cs`, `TracerouteEngine.cs`). Restrict concurrent in-flight socket/ping tasks (typically 50–100) to prevent ephemeral port exhaustion and CPU thread starvation.
- **Socket & Ping Recycling**: Never instantiate unmanaged sockets or `Ping` instances in unbounded loops without explicit disposal. Reuse clients where possible or dispose immediately via `using` blocks.
- **Zero-Allocation Packet Dissection**: Use `Span<byte>`, `ReadOnlySpan<byte>`, and `System.Buffers.ArrayPool<byte>.Shared` when parsing raw network frames, mDNS headers, SSDP packets, and DHCP options. Avoid unnecessary `byte[]` heap allocations.
- **CancellationToken Propagation**: ALL background scanning loops and async network methods MUST accept and observe a `CancellationToken`. Check `cancellationToken.ThrowIfCancellationRequested()` frequently to guarantee instant scan cancellation.
- **Canvas Rendering Optimization**: In [`RadarCanvas.cs`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/UI/RadarCanvas.cs) and [`UptimeChartControl.cs`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/UI/UptimeChartControl.cs), throttle redraw invalidations to 60fps/120fps max. Cache `SKPaint` and `SKPath` objects instead of recreating them on every draw cycle.
- **UI Thread Offloading**: Network sweeps, port probing, ARP table resolution, and LiteDB operations MUST execute on background worker threads (`Task.Run()`). Dispatch to the UI thread ONLY when updating bound observable collections via `Dispatcher.UIThread.Post(...)`.
- **Fast OUI & Vendor Lookups**: Maintain pre-indexed in-memory dictionary trees for MAC OUI resolution (`VendorLookup.cs`, `OuiDatabase.cs`) to ensure $O(1)$ lookup times during sweeps.
