# Security Policy: NodeRadar Pro

**PyPie Studio** is committed to providing a secure, reliable, and privacy-focused network reconnaissance toolkit. This document outlines our security posture, supported versions, and the protocols for reporting vulnerabilities.

---

## 🛡️ Supported Versions

We only provide security updates for the latest stable release of NodeRadar Pro. Users are encouraged to utilize the built-in **auto-update** feature to ensure they are always running the latest protected binary.

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | ✅ Yes              |
| < 1.0   | ❌ No               |

---

## 🔒 Privacy & Data Sovereignty

NodeRadar Pro is engineered with a **Local-Only Mandate**:
- **Zero Data Transmission:** No network discovery data, MAC addresses, IP logs, or device telemetry is ever transmitted to PyPie Studio or any third-party servers.
- **Local Storage:** All discovered data is stored exclusively on your local machine in an encrypted database.
- **Opt-In Alerts:** Email notifications (SMTP) only transmit data to the user-configured server.

---

## 🏗️ Technical Protections

Our architecture includes several enterprise-grade security layers to protect both your data and the integrity of the application.

### 1. Database Encryption (AES-256)
All local data (Device Inventory, Logs, and Alerts) is stored in a **LiteDB** instance protected by **AES-256 encryption**. This prevents unauthorized access to the database file even if the host machine is compromised.

### 2. SHA256 Integrity Shield
NodeRadar Pro performs a self-audit on every startup:
- It calculates a **SHA256 hash** of the core database and configuration files.
- It verifies these hashes against "Last Known Good" signatures to detect unauthorized tampering or file corruption.
- Discrepancies are logged in the **System Telemetry** for immediate audit by the administrator.

### 3. Native Binary Protection
The application is compiled using **.NET 10 NativeAOT (Ahead-of-Time)**:
- This produces a self-contained, machine-code binary rather than intermediate IL.
- It significantly reduces the attack surface and makes traditional .NET decompilation and reverse-engineering nearly impossible.

---

## 🐛 Reporting a Vulnerability

If you discover a security vulnerability in NodeRadar Pro, please do **not** open a public GitHub Issue. Instead, follow our responsible disclosure process:

1. **Email:** Send a detailed report to **security@pypiestudio.com**.
2. **Details:** Include the version of NodeRadar Pro, your OS, and a reproducible Proof of Concept (PoC).
3. **Response:** We will acknowledge your report within 48 hours and provide a timeline for the fix.

We kindly ask that you refrain from public disclosure until we have had the opportunity to verify and patch the issue.

---

**PyPie Studio**
*High-Performance Systems. Hardened Security.*
