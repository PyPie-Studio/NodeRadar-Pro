## NodeRadar Pro v1.0.0 (2026-08-28)

### New Features
- add -Push switch and automated changelog commit to Deploy-NodeRadar.ps1
- enhance PortScanner throttling, UpdateService SemVer parsing, and constant-time integrity verification
- add GitHub Actions CI workflow, test.runsettings, and Test-MasterGate -WithTests
- onboard Central Package Management and phase-gated deploy pipeline

### Maintenance and Refactoring
- ci: remove GitHub Actions workflows in favor of 100% local-first CI/CD pipeline
- chore(deploy): add automated deploy.bat and optimize packaging scripts
- chore(deploy): replace legacy publish_pro.bat with unified deploy.bat pipeline
- test(ui): remediate UptimeChartControl tests and upgrade to xunit.v3
- test: add unit tests for UptimeChartControl
- test: add unit tests for UptimeChartControl
- 🧹 format: remove unused usings in InventoryPage
- 🔒 Fix Command Injection vulnerability in InventoryPage web UI button
- 🔒 Fix Command Injection vulnerability in InventoryPage web UI button
- 🧹 format: remove unused usings in AppUtilsTests
- test: resolve rebase conflict and merge AppUtilsTests.cs
- test: add AppUtils.GetMacAddress and GetLocalIpAddress with tests
- 🧹 format: clean up unused usings across tests
- 🔒 fix: use PBKDF2 with random salt for fallback AES-GCM and fix tests
- 🔒 strengthen key derivation using PBKDF2 and random salt for AES-GCM fallback
- 🔒 fix insecure base64 fallback for smtp password encryption
- 🔒 [security fix] Add X509Chain validation for update signature certificate
- 🔒 [security fix] Verify Authenticode signature before executing update executable

### Verification and Installation Checksums

| File | Size | SHA-256 Checksum |
| :--- | :--- | :--- |

| `NodeRadar Pro_v1.0.0_Setup.exe` | 43.06 MB | `E3A5E9073E8269D11DDBDFAE24574F2703AA8C04C8139BB55A484E8674C96413` |


## deploy-2026.08.28-2030
- 893716f feat(core): enhance PortScanner throttling, UpdateService SemVer parsing, and constant-time integrity verification
- 3e14187 feat(ci): add GitHub Actions CI workflow, test.runsettings, and Test-MasterGate -WithTests
- f29b6b2 feat(automation): onboard Central Package Management and phase-gated deploy pipeline

## deploy-2026.08.28-2029
- 893716f feat(core): enhance PortScanner throttling, UpdateService SemVer parsing, and constant-time integrity verification
- 3e14187 feat(ci): add GitHub Actions CI workflow, test.runsettings, and Test-MasterGate -WithTests
- f29b6b2 feat(automation): onboard Central Package Management and phase-gated deploy pipeline
# NodeRadar Pro Changelog

## deploy-2026.08.28-2007
- 4e87d32 chore(deploy): add automated deploy.bat and optimize packaging scripts
- 9e39b43 chore(deploy): replace legacy publish_pro.bat with unified deploy.bat pipeline
- 13b0c88 test(ui): remediate UptimeChartControl tests and upgrade to xunit.v3
- aafd15c test: add unit tests for UptimeChartControl
- f9e916e test: add unit tests for UptimeChartControl
- 75a76f0 🧹 format: remove unused usings in InventoryPage
- 0059bbf 🔒 Fix Command Injection vulnerability in InventoryPage web UI button
- 7b66075 🔒 Fix Command Injection vulnerability in InventoryPage web UI button
- 9ffb4a3 🧹 format: remove unused usings in AppUtilsTests
- b9d96ec test: resolve rebase conflict and merge AppUtilsTests.cs
- 94e5d6e test: add AppUtils.GetMacAddress and GetLocalIpAddress with tests
- d4aa0d3 🧹 format: clean up unused usings across tests
- 9311daf 🔒 fix: use PBKDF2 with random salt for fallback AES-GCM and fix tests
- 9c7caa8 🔒 strengthen key derivation using PBKDF2 and random salt for AES-GCM fallback
- 0441bf6 🔒 fix insecure base64 fallback for smtp password encryption
- 520e301 🔒 [security fix] Add X509Chain validation for update signature certificate
- c60fe69 🔒 [security fix] Verify Authenticode signature before executing update executable
- 94d711b 🧪 [test] add unit tests for central dictionary UpdateNode extension


