# NodeRadar Pro Engineering Roadmap

This roadmap tracks feature development, protocol expansions, and hardening milestones for **NodeRadar Pro**. Architectural targets and protocol mechanics are guided by [`NetworkAnalysis.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/NetworkAnalysis.md).

---

## 🎯 Milestone 1: Multi-Protocol Discovery & Heuristics (In Progress)
- [x] High-speed ARP resolution via Win32 `SendARP` API.
- [x] Multi-threaded ICMP Ping Sweeps with latency & jitter telemetry.
- [x] Basic TCP service port scanner (SSH, RDP, SMB, HTTP/S, DBs).
- [ ] **mDNS / Bonjour Service Discovery:** Multicast UDP 5353 query engine extracting hostnames, device types, and TXT record metadata (`_http._tcp.local`, `_ipp._tcp.local`, `_airplay._tcp.local`).
- [ ] **SSDP / UPnP Discovery Engine:** Multicast UDP 1900 `M-SEARCH` parser extracting UPnP device models, manufacturer info, and XML device descriptors.
- [ ] **WS-Discovery Protocol:** Multicast UDP 3702 probe extraction for Windows network endpoints and IP camera discovery.
- [ ] **Passive DHCP Fingerprinting:** Sniffing UDP 67/68 frames for Option 55 (Parameter Request List) and Option 60 (Vendor Class Identifier).

---

## 🛡 Milestone 2: Security & Sentinel Enhancements
- [x] Real-time intrusion detection for unregistered MAC addresses.
- [x] Windows DPAPI key encryption for local credentials.
- [x] AES-256 encrypted LiteDB v5 database.
- [x] SHA-256 self-integrity audit on application startup.
- [ ] Network vulnerability heuristic engine matching open port signatures against CVE risk levels.
- [ ] TLS certificate metadata inspection on discovered HTTPS endpoints (expiration, SANs, issuer).

---

## ⚡ Milestone 3: Performance, Engine Optimization & Packaging
- [x] Obfuscar IL code protection.
- [x] Inno Setup 6 installer with dual-mode installation (All-Users & Current-User).
- [x] Static analysis hardening via Roslynator & SonarAnalyzer (`TreatWarningsAsErrors=true`).
- [ ] Zero-allocation packet dissection using `System.Buffers.ArrayPool<byte>` and `Span<byte>`.
- [ ] Automated regression test suite using xUnit v3 (`NodeRadarPro.Tests`).
- [ ] Enhanced high-DPI scaling and 120Hz canvas rendering optimizations for `RadarCanvas.cs`.

---

## 📐 Completed Architectural Decisions
See [`docs/decisions.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/docs/decisions.md) for full Architectural Decision Records (ADRs).
