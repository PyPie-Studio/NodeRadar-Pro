# NodeRadar Pro Engineering Roadmap

This roadmap tracks feature development, protocol expansions and hardening milestones for NodeRadar Pro.

---

## Milestone 1: Multi-Protocol Discovery and Heuristics
- [x] High-speed ARP resolution via Win32 `SendARP` API
- [x] Multi-threaded ICMP ping sweeps with latency and jitter telemetry
- [x] Service port scanner (SSH, RDP, SMB, HTTP/S, databases)
- [x] mDNS / Bonjour service discovery (`_http._tcp.local`, `_ipp._tcp.local`, `_airplay._tcp.local`)
- [x] SSDP / UPnP discovery engine (UPnP device models, manufacturer info and XML device descriptors)
- [x] NetBIOS Name Service (NBNS) query and transaction ID randomization
- [ ] WS-Discovery protocol (UDP 3702 probes for Windows endpoints and IP cameras)
- [ ] Passive DHCP fingerprinting (UDP 67/68 frames for Option 55 and Option 60)

---

## Milestone 2: Security and Sentinel Enhancements
- [x] Real-time intrusion detection for unregistered MAC addresses
- [x] Windows DPAPI key encryption for local credentials
- [x] AES-256 encrypted LiteDB v5 database
- [x] SHA-256 self-integrity audit on application startup
- [ ] Network vulnerability heuristic engine matching open port signatures against CVE risk levels
- [ ] TLS certificate metadata inspection on discovered HTTPS endpoints (expiration, SANs, issuer)

---

## Milestone 3: Performance, Engine Optimization and Packaging
- [x] Obfuscar IL code protection
- [x] Inno Setup 6 installer with dual-mode installation (All-Users and Current-User)
- [x] Static analysis hardening via Roslynator and SonarAnalyzer (`TreatWarningsAsErrors=true`)
- [x] Automated regression test suite on xUnit v3 (`tests/NodeRadarPro.Tests`)
- [ ] Zero-allocation packet dissection using `System.Buffers.ArrayPool<byte>` and `Span<byte>`
- [ ] Canvas rendering optimizations and high-DPI scaling benchmarks for `RadarCanvas.cs`

---

## Architectural Decisions
See [`docs/decisions.md`](docs/decisions.md) for full Architectural Decision Records (ADRs).
