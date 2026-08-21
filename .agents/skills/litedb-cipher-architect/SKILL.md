---
name: litedb-cipher-architect
description: Embedded LiteDB v5 database management, AES-256 database password management via DPAPI, schema migrations, and indexing strategies.
---

# litedb-cipher-architect

This skill governs embedded persistence, schema evolution, and encryption handling for **NodeRadar Pro** ([`Data/LocalDatabase.cs`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/Data/LocalDatabase.cs)).

## 🔐 Encryption Architecture
- **Engine:** LiteDB v5 with built-in AES-256 encryption.
- **Key Protection:** The database password is generated and encrypted using Windows DPAPI (`ProtectedData.Protect` with `DataProtectionScope.CurrentUser`).
- **Connection String:** Configured with `Filename=...;Password=...;Connection=shared;`.

## 📦 Collections & Schema Design
- `NetworkNode`: Network asset inventory, IP, MAC, hostname, manufacturer, OS fingerprint, status, open ports, last seen timestamp.
- `AlertEvent`: Intrusion alerts, rogue MAC detection, status change logs.
- `LogEntry`: System and diagnostic log history.
- `AppSettings`: User configuration, SMTP credentials, scan timeouts.

## ⚡ Indexing & Performance
- Ensure unique index on `NetworkNode.MacAddress` and index on `NetworkNode.IpAddress`.
- Index `AlertEvent.Timestamp` and `LogEntry.Timestamp` descending for rapid log queries.
- Synchronize access to LiteDB instances to prevent multi-threaded file lock contention.
