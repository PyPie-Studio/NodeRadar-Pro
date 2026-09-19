<div align="center">

# NodeRadar Pro

Desktop network observability, host discovery and latency monitoring for Windows.  
Discovers LAN devices via multi-threaded ARP and ICMP sweeps, tracks live jitter and alerts on unregistered MAC addresses — local-only, zero kernel drivers.

[![GitHub release](https://img.shields.io/github/v/release/PyPie-Studio/NodeRadar-Pro?style=for-the-badge&logo=github&color=6B21A8)](https://github.com/PyPie-Studio/NodeRadar-Pro/releases/latest)
[![CI](https://img.shields.io/github/actions/workflow/status/PyPie-Studio/NodeRadar-Pro/ci.yml?branch=main&style=for-the-badge&logo=githubactions&logoColor=white&label=CI)](https://github.com/PyPie-Studio/NodeRadar-Pro/actions)
[![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=for-the-badge&logo=windows11&logoColor=white)](https://github.com/PyPie-Studio/NodeRadar-Pro)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE.md)

[Download](https://github.com/PyPie-Studio/NodeRadar-Pro/releases/latest) · [Changelog](CHANGELOG.md) · [Roadmap](ROADMAP.md) · [Contributing](CONTRIBUTING.md)

</div>

---

<div align="center">
  <img width="1200" alt="NodeRadar Pro Dashboard" src="https://github.com/user-attachments/assets/28b22b37-af3d-4995-a71f-d2579a6027d9" />
</div>

<details>
<summary>Click to view more screenshots (Subnet Scanner, Port Scanner, Traceroute, Settings, Logs)</summary>

<br>

<p align="center">
  <em>Subnet Scanner with multi-threaded sweeps and real-time host discovery</em><br>
  <img width="1200" alt="Subnet Scanner" src="https://github.com/user-attachments/assets/d8bd0a99-c86e-4b71-9df7-0c2889c709c4" />
</p>

<p align="center">
  <em>Service Port Scanner with concurrent socket probes and OS fingerprinting</em><br>
  <img width="1200" alt="Port Scanner" src="https://github.com/user-attachments/assets/91941e18-3eb6-44ea-af28-128cd359a103" />
</p>

<p align="center">
  <em>Visual Traceroute engine with hop-by-hop latency mapping</em><br>
  <img width="1200" alt="Visual Traceroute" src="https://github.com/user-attachments/assets/6f848a9d-5ad3-4ec0-a6e2-f89808ea904f" />
</p>

<p align="center">
  <em>System Configurations for scan parameters, notification routing and thresholds</em><br>
  <img width="1200" alt="System Configurations" src="https://github.com/user-attachments/assets/95c7fdb5-aace-4706-aaca-449f71043f84" />
</p>

<p align="center">
  <em>System Telemetry with event logs, integrity verification and exportable audit trails</em><br>
  <img width="1200" alt="System Logs" src="https://github.com/user-attachments/assets/9aaec90c-0028-4975-8ef3-2d3284780b85" />
</p>

</details>

---

## Download and Quickstart

NodeRadar Pro is available as an installer package or can be compiled directly from source.

### Option 1: Installer Package

1. Go to the [Latest Release](https://github.com/PyPie-Studio/NodeRadar-Pro/releases/latest) page.
2. Download `NodeRadar_Pro_Setup.exe`.
3. Run the installer. Choose between two deployment modes:
   - **Current User (Recommended)**: Installs to `%LOCALAPPDATA%\Programs\PyPie Studio\NodeRadar Pro\`. Requires no administrator privileges or UAC elevation.
   - **All Users**: Installs to `C:\Program Files\PyPie Studio\NodeRadar Pro\`. Requires administrator elevation and creates system-wide start menu shortcuts.

### Option 2: Build from Source

Requirements: Windows 10/11 and [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
# Clone repository
git clone https://github.com/PyPie-Studio/NodeRadar-Pro.git
cd NodeRadar-Pro

# Build in Release mode
dotnet build NodeRadarPro.slnx --configuration Release

# Run test suite (320+ unit and integration tests)
dotnet test NodeRadarPro.slnx --configuration Release --nologo

# Launch application
dotnet run --project src/NodeRadarPro/NodeRadarPro.csproj --configuration Release
```

---

## What It Does

NodeRadar Pro provides a unified console for local network monitoring and device diagnostics:

- **Subnet Sweep and Host Discovery**: Scans IPv4 ranges using multi-threaded ICMP ping sweeps and direct Win32 `SendARP` table queries. Probing is throttled via `SemaphoreSlim` (64 in-flight sockets) to prevent ephemeral port exhaustion (`WSAENOBUFS 10055`).
- **Service Port Scanner**: Probes common service ports (SSH, RDP, SMB, HTTP/S, databases) on target hosts. Uses parallel connection probes with short-circuit timeout cancellation so unresponsive ports complete concurrently (~200ms) rather than blocking sequentially.
- **Intrusion Sentinel**: Background monitoring engine that compares discovered MAC addresses against your authorized device inventory. Triggers visual alerts, local audio indicators and opt-in SMTP notifications the moment an unregistered device connects to the subnet.
- **Continuous Latency and 24h Uptime Pulse**: Tracks millisecond-level round-trip latency, jitter and packet loss over time. Displays a visual 24-hour sliding-window uptime bar for every monitored node.
- **Heuristic Device Classification**: Identifies device models and operating systems through combined heuristics: IEEE MAC OUI manufacturer database, mDNS Bonjour records (`_http._tcp.local`, `_ipp._tcp.local`), SSDP/UPnP XML descriptors, NetBIOS Name Service (NBNS) queries and open port fingerprints.
- **Multi-Hop Traceroute**: Interactive ICMP traceroute showing intermediate hops, per-hop round-trip times and host resolution.
- **Encrypted Local Storage**: Device inventories, alert histories and diagnostic logs are persisted locally in an embedded LiteDB database encrypted with AES-256. Database encryption keys and SMTP credentials are encrypted via Windows DPAPI (`DataProtectionScope.CurrentUser`).

---

## Privacy and Local-Only Mandate

NodeRadar Pro is built on strict data sovereignty rules:

| Invariant | Implementation |
| :--- | :--- |
| **Zero External Telemetry** | No usage data, scan results, IP addresses or MAC tables are transmitted to PyPie Studio or third-party servers. |
| **No Kernel Drivers** | Uses user-mode Win32 networking primitives and standard socket APIs. Does not require Npcap, WinPcap or driver installation. |
| **Encrypted at Rest** | All local inventories, credentials and logs are stored in `%Documents%\PyPie Studio\NodeRadar Pro\noderadar.db` using AES-256 encryption. |
| **DPAPI Key Storage** | Encryption master keys are protected using the Windows Data Protection API tied to the active Windows user account. |
| **No Background Network Calls** | The application never contacts external servers on startup or in the background. Update checks are initiated solely by the user visiting the GitHub Releases page. |

---

## Why I Built This

Most network scanners on Windows fall into two categories:

- **Command-line packet sniffers (like Nmap)**: Extremely capable, but they rely on kernel-level packet filter drivers (Npcap/WinPcap), require elevation for basic sweeps and do not provide continuous background telemetry or uptime history in a lightweight desktop UI.
- **Freeware GUI scanners (like Advanced IP Scanner or Angry IP)**: Fast for a one-time sweep, but they are closed-source, lack continuous background intrusion monitoring, offer no automated encrypted storage and frequently come bundled with promotional software.

NodeRadar Pro was built to bridge this gap for systems administrators, homelab maintainers and network engineers:
- **Pure C# and Avalonia UI**: Native desktop interface with dark-purple glassmorphic styling, responsive layout and SkiaSharp radar visualization.
- **User-mode discovery**: Sub-second subnet sweeping via native Win32 `SendARP` without requiring kernel driver installation or administrator rights.
- **Continuous monitoring**: Runs in the background, tracks jitter and latency over 24 hours and alerts when unfamiliar hardware connects.
- **100% Local**: No accounts, no cloud dashboards and no telemetry leakage.

---

## Supported Environments

| OS Version | Build Range | Status |
| :--- | :--- | :--- |
| **Windows 11 25H2** | Build 26200+ | Supported |
| **Windows 11 24H2** | Build 26100–26120 | Supported |
| **Windows 11 23H2 / 22H2** | Build 22631 / 22621 | Supported |
| **Windows 10 22H2** | Build 19045 | Supported |
| **Windows 10 Enterprise LTSC** | Build 19044 / 17763 | Supported |

---

## Cryptographic Verification

Official release installers published on GitHub Releases include SHA-256 checksums in `SHA256SUMS.txt`. You can verify installer integrity locally in PowerShell:

```powershell
Get-FileHash NodeRadar_Pro_Setup.exe -Algorithm SHA256
```

---

## Documentation

- [Architecture Decision Records (ADRs)](docs/decisions.md) - Architectural history, design decisions and protocol standards.
- [Benchmark Report](docs/benchmarks.md) - Empirical scan throughput, memory footprint, persistence and SkiaSharp UI latency.
- [Troubleshooting Guide](docs/troubleshooting.md) - Multi-NIC precedence, Windows Firewall rules, multicast discovery and VPN split-tunneling.
- [Code Coverage Report](docs/coverage-report.md) - Layer-by-layer test coverage metrics.

---

## Contributing

Contributions are welcome. Please review [CONTRIBUTING.md](CONTRIBUTING.md) for build instructions, local development guidelines and Master Quality Gate requirements before opening a pull request. All participants are expected to adhere to our [Code of Conduct](CODE_OF_CONDUCT.md).

- **Bug Reports**: Open an issue describing the steps to reproduce, Windows version and network topology.
- **Feature Proposals**: Submit an issue detailing the technical use case.
- **Pull Requests**: Ensure all changes pass `scripts/Test-MasterGate.ps1 -WithTests` with zero compiler warnings and clean formatting.

---

## License

MIT License. Copyright (c) 2026 PyPie Studio. See [LICENSE.md](LICENSE.md) for full terms.

