---
name: network-engine-architect
description: Low-level network discovery engine procedures, Win32 SendARP API, ICMP multi-threading, TCP port sweeps, packet buffer pooling, and NetworkAnalysis.md heuristics.
---

# network-engine-architect

This skill governs low-level network discovery, packet dissection, and device identification algorithms in **NodeRadar Pro** ([`Core/`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/Core)).

## 🛰 Discovery Protocols & Mechanics
Consult [`NetworkAnalysis.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/NetworkAnalysis.md) for protocol specifications:

1. **Layer-2 ARP Resolution:**
   - Win32 `SendARP` API (`ArpResolver.cs`) resolves IP $\rightarrow$ MAC mappings instantaneously without elevated privileges.
   - Match MAC prefixes (OUI) against `OuiDatabase.cs` and `VendorLookup.cs` for hardware manufacturer resolution.
2. **Layer-3 ICMP Sweeping:**
   - Multi-threaded ping probes with millisecond-accurate round-trip latency (`Ping.SendPingAsync`).
   - Bound concurrency with `SemaphoreSlim` (max 50–100 simultaneous probes) to prevent socket starvation.
3. **Layer-4 Service Port Auditing:**
   - Asynchronous TCP connect probing (`TcpClient.ConnectAsync`) on critical ports (SSH 22, Telnet 23, HTTP 80, HTTPS 443, SMB 445, RDP 3389, MySQL 3306, Postgres 5432).
   - Enforce tight connection timeouts (250ms–750ms) to ensure rapid subnet completion.
4. **Multicast Discovery & Heuristic Fingerprinting:**
   - **mDNS (UDP 5353):** Multicast DNS-SD queries (`224.0.0.251`) for `_http._tcp.local`, `_ipp._tcp.local`, `_airplay._tcp.local`.
   - **SSDP (UDP 1900):** `M-SEARCH` broadcast queries (`239.255.255.250`) to parse UPnP device descriptors and XML root devices.
   - **DHCP Option Sniffing (UDP 67/68):** Passive mapping of Option 55 (Parameter Request List) and Option 60 (Vendor Class Identifier).
   - **WS-Discovery (UDP 3702):** Probe requests (`239.255.255.250`) for Windows network devices and ONVIF cameras.

## 🔒 Concurrency & Memory Rules
- Always pass `CancellationToken` to all asynchronous operations.
- Reuse buffers via `System.Buffers.ArrayPool<byte>.Shared` when parsing raw packet streams.
- Always dispose sockets and `TcpClient` instances in `finally` blocks or `using` scopes.
