---
trigger: always_on
description: Security enforcement - DPAPI encryption, LiteDB AES-256, path sanitization with Path.GetFullPath, constant-time comparisons, non-admin socket fallback, startup SHA-256 self-audit, and generic error exposure.
alwaysApply: true
---
# Security Guardrails

- **DPAPI Credential Protection**: ALL local credentials, API tokens, and database encryption keys MUST use `System.Security.Cryptography.ProtectedData` (Windows DPAPI with `DataProtectionScope.CurrentUser`). Never persist plaintext secrets.
- **LiteDB Encryption**: The local embedded LiteDB database (`LocalDatabase.cs`) MUST always be protected with AES-256 database password encryption.
- **Path Sanitization**: ALWAYS use `Path.GetFullPath()` and verify that the target path starts with the authorized base directory + `Path.DirectorySeparatorChar`. Block directory traversal attacks (`../`, `..\`) in diagnostic log exporters and updater scripts.
- **Constant-Time Comparison**: Use `CryptographicOperations.FixedTimeEquals()` when comparing secret tokens, database hashes, or integrity checksums. Never use `==` or `.Equals()` for secret comparison.
- **Non-Admin Permission Fallback**: Never crash if elevated/admin rights are missing. Fallback gracefully from raw socket packet sniffing to standard user-mode Win32 `SendARP` or standard TCP/UDP socket probes.
- **Binary Self-Integrity Shield**: Preserve and verify SHA-256 executable integrity hashing on application startup (`SecurityService.cs`) to prevent unauthorized binary modification.
- **Information Exposure**: Diagnostic log exports, crash logs, and UI notifications must NEVER expose internal credential paths, private decryption keys, or unhandled exception stack traces to external logs.
