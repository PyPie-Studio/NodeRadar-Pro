# NodeRadar Pro by PyPie Studio
### Enterprise-Grade Network Observability & Asset Discovery

**NodeRadar Pro** is a high-performance, low-latency network monitoring utility designed for Senior IT Administrators and System Engineers. Engineered for speed and precision, it provides an instantaneous "radar" view of enterprise-scale LAN environments, capable of mapping and fingerprinting hundreds of assets in seconds.

---

## 🛰️ Core Capabilities

- **Deep Network Discovery:** High-speed subnet sweeping utilizing multi-threaded ARP and ICMP probes to identify all active assets.
- **Intrusion Sentinel:** Real-time background detection that triggers alerts the moment an unauthorized or unknown MAC address appears on the network.
- **Precision Telemetry:** Millisecond-accurate ICMP engine with dynamic jitter analysis and live packet loss tracking.
- **Dynamic Uptime Intelligence:** A sliding-window uptime history that provides a 24-hour visual "pulse" of every registered device.
- **Deep Intelligence Engine:** High-accuracy heuristic OS fingerprinting utilizing weighted signals (MAC, Ports, mDNS, SSDP) to identify specific device types (iPhones, Windows PCs, Printers, IoT) and hardware vendors.
- **Proactive Port Auditing:** Integrated service scanner for identifying open vectors like SSH, RDP, HTTP/S, and database ports.

---

## 🏗️ Architecture & Performance

NodeRadar Pro was engineered from the ground up to satisfy the rigorous requirements of high-density infrastructure.

- **Production-Ready Architecture:** Built on **C# 14** and **.NET 10**, the core utilizes an Event Aggregator pattern and Concurrent Collections for thread safety. Socket leak fixes and CancellationToken support ensure infinite 100% fluid uptime even during massive sweeps.
- **Native Performance:** Compiled via **.NET NativeAOT**, producing a zero-dependency, machine-code binary with near-instant startup and a specialized low-memory footprint.
- **DPI-Aware UI:** Fully optimized for **4K/5K displays** with razor-sharp high-resolution assets and support for Windows 11 **Mica and Acrylic** effects.
- **Mobile-First Resilience:** Heuristic monitoring logic that accounts for WiFi power-save modes on mobile devices, preventing false-positive disconnection alerts.

---

## 🔒 Security-First Engineering

As a professional security and monitoring tool, NodeRadar Pro includes multiple layers of hardening:

- **AES-256 Data Encryption:** All local device inventories, logs, and telemetry are stored in an encrypted **LiteDB** instance.
- **SHA256 Integrity Shield:** Performs an automated self-audit on every startup, verifying binary and database signatures to detect tampering.
- **Zero-Trust Privacy:** **Local-Only Mandate.** No network data, MAC addresses, or infrastructure details ever leave your machine.
- **Automated Updates:** Seamless background downloading and silent installation of new versions, keeping the system continuously protected.
- **Automated Maintenance:** Scheduled auto-backup timers and database restoration capabilities.
- **Forensic Logs:** Comprehensive system telemetry with searchable and exportable audit trails.

---

## 📦 Installation & Deployment

NodeRadar Pro is distributed as a compiled, hardened binary. 

### Getting Started
1. Navigate to the [Releases](https://github.com/PyPie-Studio/NodeRadar-Pro/releases) tab.
2. Download the **Setup.exe** for a professional installation experience to Program Files.
3. For portable use, the **Single-File EXE** is available for zero-install deployment.

### "Pro" Interactions
- **Click-to-Copy:** Instantly copy IP or MAC addresses to your clipboard with a single click.
- **🌐 Open Web UI:** Jump directly to a router or IP camera's web interface from the device detail panel.
- **SMTP Alerts:** Configure encrypted email notifications in Settings for real-world alerting.

---

## 🥧 About PyPie Studio

**PyPie Studio** specializes in high-performance desktop tools for systems engineers. We focus on modern .NET paradigms, hardened security, and premium UI/UX to build software that IT professionals trust.

*Interested in our tech stack or looking to collaborate? Reach out via our GitHub profile.*
