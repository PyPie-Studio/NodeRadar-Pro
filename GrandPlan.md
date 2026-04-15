# NodeRadar Pro - Grand Plan

**Developer:** PyPie Studio
**Target Audience:** IT Admins, Sysadmins, Homelab Enthusiasts (Windows & Linux)
**Business Model:** One-time global purchase (<$100)
**Tech Stack:** C# 14, .NET 10, Avalonia UI (Pure C#, No XAML), LiteDB
**UI/UX Theme:** Modern dark purple (`#1A1A2E`) and dark grey accents

---

## 🚀 Phase 1: The Core Foundation (No UI yet)
1. **The Engine (`NodeRadar.Core`):** 
   - Pure C# 14 class library for heavy lifting.
   - Use `System.Net.NetworkInformation` to ping subnets asynchronously.
   - Resolve MAC addresses natively (ARP on Windows, `/proc/net/arp` parsing on Linux).
2. **The Brain (`NodeRadar.Data`):** 
   - Integrate LiteDB to store historical device records locally.
   - Remember devices across sessions (custom names, icons, IP history).

## 🎨 Phase 2: The Modern UI (Avalonia C#)
1. **The Canvas (`NodeRadar.UI`):** 
   - Delete all default Avalonia XAML files. 
   - Write a pure C# fluent UI builder.
2. **The Aesthetic:** 
   - Implement PyPie Studio's signature dark purple and dark grey themes.
3. **The Radar:** 
   - Write a custom Avalonia `DrawingContext` control.
   - Physically draw a sweeping radar animation with glowing dots for active IPs.

## ⚙️ Phase 3: The Pro Features (The $49 Value)
1. **Intrusion Alerts:** 
   - Run silently in the background. 
   - Desktop notification when a new, unknown MAC address joins the LAN.
2. **Vendor Lookup:** 
   - Embed an offline OUI database (JSON file of MAC prefixes).
   - Instantly identify device manufacturers (e.g., "Apple", "Cisco", "Sony").
3. **Port Scanning:** 
   - Right-click option on any node to scan common ports (80, 443, 22, 3389) to identify running services.

## 📦 Phase 4: Deployment
1. **Windows:** 
   - Compile standalone `.exe`.
   - Wrap in an Inno Setup installer (`.iss`).
2. **Linux:** 
   - Compile standalone Linux binary (`AppImage` or `.deb`).
   - Ready for Arch, Kali, Debian, and Ubuntu sysadmins.