# NodeRadar Pro - Grand Plan (Revised)

**Developer:** PyPie Studio
**Business Model:** One-time global purchase (<$100)
**Tech Stack:** C# 14, .NET 10, Avalonia UI (Pure C#), LiteDB, Visual Studio
**UI/UX Theme:** Modern Dark Purple (`#2D004D`) and Dark Gray (`#121212`)

---

## ✅ Completed Foundations
- **Core Engine:** Subnet scanning, ARP resolution (Windows/Linux), and connectivity monitoring.
- **Data Layer:** Basic LiteDB integration for device history, logs, and alerts.
- **UI Shell:** Pure C# Avalonia implementation of Sidebar, TopNav, and Page switching.
- **Prototyping:** Dark theme established with Stitch; pages for Dashboard, Scanner, Inventory, and Alerts are functional.
- **Vendor Intelligence:** Integrated OUI lookup database for hardware identification.

---

## 🛠️ Production Readiness (Current Focus)

### 1. Stability & Performance (Critical)
- [x] **Core Refactoring:** Refactor the codebase for maintainability and performance.
- [x] **Event Aggregator:** Implement Event Aggregator for decoupled service communication.
- [x] **Socket Leak Fixes:** Implement CancellationToken support for infinite fluid uptime.
- [x] **Singleton Database:** Refactor `LocalDatabase` to use a shared connection to eliminate file-locking crashes.
- [x] **Async Init:** Ensure all service starts (Scanner/Monitor) are non-blocking for instant UI launch.
- [x] **Global Error Handling:** Implement a crash-reporting system that logs to `PyPie Studio\NodeRadar Pro\logs`.

### 2. Branding & UI Polish
- [x] **Logo Integration:** 
  - Set the app icon in `AppBuilder`.
  - Display the PyPie Studio logo in the Sidebar or About page.
- [x] **Mica/Acrylic:** Apply Windows 11 transparency effects to the `DarkPurpleTheme`.
- [x] **DPI Awareness:** Audit custom controls (`RadarCanvas`, `UptimeChart`) for perfect scaling on 4K monitors.

### 3. Feature Completion
- [x] **Intrusion Logic:** Finalize the background logic that triggers `IntrusionAlerter` when a new MAC is seen.
- [x] **Sound Alerts:** Implement "Premium" alert sounds for critical disconnections.
- [x] **Email Alerts:** Add SMTP configuration and sending logic to `AppSettings`.
- [x] **Security:** Implement SHA256 integrity checks for the database and core logic.
- [x] **Automated Updates:** Background downloading and silent installation of new releases.
- [x] **Intelligent Recognition:** Deep Intelligence engine using weighted signals for Nmap-level accuracy.

---

## 🚀 Future Roadmap
- **Network Topology Visualization:** Interactive map of network structures.
- **Advanced Vulnerability Scanning:** Deep service probing for specific CVEs.

---

## 📦 Deployment & Sale
- [x] **Code Protection:** Apply obfuscation to prevent reverse-engineering of the core scanning logic.
- [x] **Installer:** 
  - Choice B: **Inno Setup** (Finalized for professional installation to Program Files).
  - Choice C: **GitHub Releases** (Manual update check integration in Settings).
- [x] **Production Build:** Optimize for **ReadyToRun** and **Modular Publish** (to support Obfuscar logic protection) for fast startup and hardened security.
