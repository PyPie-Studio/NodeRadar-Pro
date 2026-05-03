# NodeRadar Pro by PyPie Studio
### Enterprise-Grade Network Observability & Asset Discovery

**NodeRadar Pro** is a high-performance, low-latency network monitoring utility designed for Senior IT Administrators and System Engineers. Engineered for speed and precision, it provides an instantaneous "radar" view of enterprise-scale LAN environments, capable of mapping and fingerprinting hundreds of assets in seconds.

---

## 🛰️ Core Capabilities

- **Deep Network Discovery:** High-speed subnet sweeping utilizing multi-threaded ARP and ICMP probes to identify all active assets.
- **Intrusion Sentinel:** Real-time background detection that triggers alerts the moment an unauthorized or unknown MAC address appears on the network.
- **Precision Telemetry:** Millisecond-accurate ICMP engine with dynamic jitter analysis and live packet loss tracking.
- **Dynamic Uptime Intelligence:** A sliding-window uptime history that provides a 24-hour visual "pulse" of every registered device.
- **Heuristic OS Fingerprinting:** Advanced probing to identify hostnames, hardware vendors (Apple, Samsung, Cisco, etc.), and OS types (Windows, Linux, iOS).
- **Proactive Port Auditing:** Integrated service scanner for identifying open vectors like SSH, RDP, HTTP/S, and database ports.

---

## 🏗️ Architecture & Performance

NodeRadar Pro was engineered from the ground up to satisfy the rigorous requirements of high-density infrastructure.

- **High-Concurrency Engine:** Built on **C# 14** and **.NET 10**, the core utilizes asynchronous non-blocking patterns. The UI remains 100% fluid even during massive 700+ device sweeps.
- **Native Performance:** Compiled via **.NET NativeAOT**, producing a zero-dependency, machine-code binary with near-instant startup and a specialized low-memory footprint.
- **DPI-Aware UI:** Fully optimized for **4K/5K displays** with razor-sharp high-resolution assets and support for Windows 11 **Mica and Acrylic** effects.
- **Mobile-First Resilience:** Heuristic monitoring logic that accounts for WiFi power-save modes on mobile devices, preventing false-positive disconnection alerts.

---

## 🔒 Security-First Engineering

As a professional security and monitoring tool, NodeRadar Pro includes multiple layers of hardening:

- **AES-256 Data Encryption:** All local device inventories, logs, and telemetry are stored in an encrypted **LiteDB** instance.
- **SHA256 Integrity Shield:** Performs an automated self-audit on every startup, verifying binary and database signatures to detect tampering.
- **Zero-Trust Privacy:** **Local-Only Mandate.** No network data, MAC addresses, or infrastructure details ever leave your machine.
- **Forensic Logs:** Comprehensive system telemetry with searchable and exportable audit trails.

---

## 📦 Installation & Deployment

NodeRadar Pro is distributed as a compiled, hardened binary. 

### Getting Started
1. Navigate to the [Releases](https://github.com/PyPie-Studio/NodeRadar-Pro/releases) tab.
2. Download the **Velopack Installer** for a premium setup experience with seamless background auto-updates.
3. For portable use, the **Single-File EXE** is available for zero-install deployment.

### "Pro" Interactions
- **Click-to-Copy:** Instantly copy IP or MAC addresses to your clipboard with a single click.
- **🌐 Open Web UI:** Jump directly to a router or IP camera's web interface from the device detail panel.
- **SMTP Alerts:** Configure encrypted email notifications in Settings for real-world alerting.

---

## 🥧 About PyPie Studio

**PyPie Studio** specializes in high-performance desktop tools for systems engineers. We focus on modern .NET paradigms, hardened security, and premium UI/UX to build software that IT professionals trust.

*Interested in our tech stack or looking to collaborate? Reach out via our GitHub profile.*
