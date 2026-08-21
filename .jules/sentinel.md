# Sentinel Journal: Security, DPAPI & Integrity Learnings

This journal records security hardening, encryption protocols, path validations, and integrity protections for **NodeRadar Pro**.

---

## 2026-08-17 — DPAPI Secure Storage for Local Credentials & Database Passwords
- **Problem**: Storing database encryption keys or SMTP passwords in plaintext JSON settings exposes credentials to local filesystem extraction.
- **Decision**: Encrypt all secrets using Windows Data Protection API (`System.Security.Cryptography.ProtectedData.Protect`) scoped to `DataProtectionScope.CurrentUser`.
- **Impact**: Machine and user-isolated credential protection. Plaintext keys never touch disk.

## 2026-08-17 — Startup SHA-256 Binary Integrity Verification
- **Problem**: Potential risk of unauthorized binary patching or tampering in sensitive network environments.
- **Decision**: Execute automated SHA-256 self-hashing on startup in `SecurityService.cs`.
- **Impact**: Instant detection of modified assemblies or compromised runtime binaries.

## 2026-08-21 — Inno Setup Dynamic Versioning, Mutex & Restart Manager
- **Problem**: Hardcoded installer version numbers create drift between binary assembly attributes and release packages. Running instances during updates can lock `.dll` files and cause partial file replacement corruption.
- **Decision**: Implemented dynamic version extraction (`GetFileVersion`) in `Inno/installer.iss` and synchronized named mutex `NodeRadarPro_App_Mutex_Active` in `Program.cs` alongside `CloseApplications=yes`.
- **Impact**: Single source of truth for versioning, clean single-instance runtime enforcement, and zero locked-file errors during updates.

## 2026-08-21 — Remote Updater Scheme/Host Whitelisting & Active Stream Security
- **Problem**: Allowing arbitrary or non-HTTPS update download URLs could expose the application to malicious binary execution or man-in-the-middle attacks. Reading from raw socket streams during TLS probing leaked plain/encrypted boundary state.
- **Decision**: Enforced HTTPS scheme, exact `github.com` host verification, and repository-scoped path boundaries in `UpdateService.cs`. Unified all TLS/plaintext socket reads onto the authenticated `activeStream` in `BannerGrabProbe.cs`.
- **Impact**: Hardened update integrity and secure multi-protocol banner grabbing.


