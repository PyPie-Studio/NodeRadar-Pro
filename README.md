# NodeRadar Pro by PyPie Studio
### Enterprise-Grade Network Observability & Asset Discovery

**NodeRadar Pro** is a high-performance, low-latency network monitoring utility designed for Senior IT Administrators, Network & System Engineers. Engineered for speed and precision, it provides an instantaneous "radar" view of enterprise-scale LAN environments, capable of mapping hundreds of assets in seconds.

---

## 🛰️ Core Capabilities

- **Deep Network Discovery:** High-speed subnet sweeping to identify all active IP assets across complex segments.
- **Real-Time ICMP Monitoring:** Millisecond-accurate pinging engine with jitter and packet loss analysis.
- **Advanced Asset Resolution:** Full MAC address resolution and OUI-based hardware vendor identification.
- **Heuristic OS & Uptime Detection:** Low-level probing to identify hostnames, operating system types, and active uptime metrics.
- **Proactive Port Scanning:** Integrated multi-threaded port scanner for auditing common service vectors (SSH, RDP, HTTP/S, SQL).
- **Intrusion Sentinel:** Background logic that triggers alerts the moment an unauthorized or unknown MAC address appears on the wire or when a device gets disconnected.

---

## 🏗️ Architecture & Performance

NodeRadar Pro was built with a "Performance First" philosophy to satisfy the requirements of modern, high-density infrastructure.

- **High-Concurrency Engine:** Built on **C# 14** and **.NET 10**, the core utilizes advanced `Task`-based asynchronous patterns and custom thread-pool management. This ensures that even during a **700+ device sweep**, the UI remains fluid and non-blocking.
- **Enterprise Battle-Tested:** Validated in live production environments with over 700 concurrent nodes without degradation in resolution accuracy.
- **Low-Level Integration:** Leveraging **WMI** (Windows Management Instrumentation) and **D-Bus** (Linux) for hardware-level telemetry and system polling.
- **Native Performance:** Compiled using **.NET NativeAOT**, producing a zero-dependency, self-contained binary with near-instant startup times and a minimal memory footprint.
- **Hardened Integrity:** Protected via **Obfuscar** implementation to ensure binary integrity and protect proprietary network-probing algorithms from reverse-engineering.

---

## 📦 Installation & Deployment

NodeRadar Pro is distributed as compiled freeware. Source code is not available in this repository.

### Getting Started
1. Navigate to the [Releases](https://github.com/PyPie-Studio/NodeRadar-Pro/releases) tab.
2. Download the latest **Installer Setup** for a premium installation experience with automatic background updates.
3. Alternatively, download the **Portable Executable (.exe)** for zero-install usage.

### Basic Usage
- **Start Scan:** Select your primary Network Interface and hit "Initiate Radar."
- **Monitor:** Right-click any discovered node to view detailed latency charts or initiate a specific port scan.
- **Alerts:** Configure SMTP settings in "Settings" to receive email dispatches when critical servers go offline.

---

## 🛠️ Support & Issue Tracking

As a closed-source freeware tool, we welcome community feedback via GitHub:
- **Bug Reports:** Please use the [Issues](https://github.com/PyPie-Studio/NodeRadar-Pro/issues) tab to report reproducible crashes, bugs and issues.
- **Feature Requests:** Suggest new probes or UI refinements via GitHub Discussions or Issues.
- **Disclaimer:** This software is provided "As-Is" by PyPie Studio. No warranty is provided, expressed, or implied.
- **Please Note that support for Linux and Mac is currently in alpha stage and not ready for production release. Follow Us On GitHub or [Social Media](https://linktr.ee/pypiestudio) Accounts For Updates**

---

## 🥧 About PyPie Studio

**PyPie Studio** is a forward-thinking development Studio specializing in high-performance C# applications, systems tools, and low-latency utilities. We believe in building software that is as beautiful as it is powerful, focusing on modern .NET paradigms and premium UI/UX design.

*Interested in our tech stack or looking to collaborate? Reach out via our GitHub profile.*

<p align="center">
  Made with ❤️ by PyPie Studio
</p>

<p align="center">
  © 2026 PyPie Studio. All Rights Reserved.
</p>
