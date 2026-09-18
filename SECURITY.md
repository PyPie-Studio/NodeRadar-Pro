# Security Policy

PyPie Studio is committed to providing a secure, reliable and privacy-focused network observation toolkit. This document outlines our security posture, supported versions and the protocol for reporting vulnerabilities.

---

## Supported Versions

We provide security updates for the latest stable release of NodeRadar Pro.

| Version | Supported |
| :--- | :--- |
| 1.6.x | Yes |
| 1.5.x | Critical fixes only |
| < 1.5 | No |

---

## Privacy and Data Sovereignty

NodeRadar Pro is engineered with a **Local-Only Mandate**:
- **Zero Remote Data Transmission:** No network discovery data, MAC addresses, IP logs or device telemetry is transmitted to PyPie Studio or any third-party telemetry services.
- **Local Storage:** Discovered nodes, port profiles and alert events are stored locally in an embedded LiteDB database.
- **Opt-In Alerting:** Email notifications (SMTP) only communicate directly with your configured mail server over SSL/TLS.
- **Update Checks:** If enabled, version checking queries the official GitHub Releases API (`api.github.com/repos/PyPie-Studio/NodeRadar-Pro/releases/latest`) solely to compare semantic versions.

---

## Technical Safeguards

1. **AES-256 Encrypted Database**: Local database storage is encrypted at rest using AES-256. Database encryption keys and SMTP credentials are protected using Windows DPAPI (`DataProtectionScope.CurrentUser`).
2. **SHA-256 Integrity Audit**: The application runs an automated self-audit on startup to verify binary signature integrity against expected checksums.
3. **No Kernel Drivers Required**: All host discovery and port probing run strictly through standard user-mode Win32 socket primitives (`SendARP`, ICMP echo, TCP connect). No kernel filter drivers or packet-sniffing drivers (such as WinPcap or Npcap) are required or installed.

---

## Reporting a Vulnerability

If you discover a security vulnerability, please do not open a public GitHub issue. Instead, report it via responsible disclosure:

1. **Email:** Send details to **security@pypiestudio.com**.
2. **Details:** Include the version of NodeRadar Pro, your Windows build number and reproduction steps or proof-of-concept.
3. **Response:** We will acknowledge receipt within 48 hours and coordinate a patch release.
