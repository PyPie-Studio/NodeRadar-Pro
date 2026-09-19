# NodeRadar Pro Code Coverage & Quality Report

> Generated automatically on **2026-09-20 01:34:27 AM** via scripts/Test-Coverage.ps1.

### Solution Coverage Overview

| Metric | Coverage Value | Status |
| :--- | :---: | :---: |
| **Total Executable Lines** | **6891 / 12918** | **53.3%** |
| **Total Decision Branches** | **2330 / 5872** | **39.7%** |
| **Active Unit Tests** | **179 Passing (100%)** | Optimal |

---

### Domain Layer Breakdown

| Architectural Layer | Files | Covered Lines | Line Coverage | Branch Coverage | Health |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Core / Discovery & Fingerprinting** | 14 | 1178 / 1563 | **75.4%** | 56.8% | Moderate |
| **Core / Engines** | 9 | 1015 / 1420 | **71.5%** | 57.5% | Moderate |
| **Core / Models & Messaging** | 4 | 64 / 103 | **62.1%** | 41.5% | Moderate |
| **Core / Services** | 7 | 376 / 604 | **62.3%** | 63.7% | Moderate |
| **Data Layer** | 4 | 4152 / 4437 | **93.6%** | 59% | Optimal |
| **Other** | 2 | 0 / 77 | **0%** | 0% | Attention Required |
| **UI / Presentation Layer** | 15 | 106 / 4714 | **2.2%** | 3% | Attention Required |

---

### Detailed Component Analysis

#### Core / Discovery & Fingerprinting

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`FingerprintResult.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/FingerprintResult.cs) | 3/3 | **100%** | Optimal |
| [`MacOuiProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/MacOuiProbe.cs) | 22/22 | **100%** | Optimal |
| [`DeviceIconMapper.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeviceIconMapper.cs) | 30/30 | **100%** | Optimal |
| [`DiscoveryEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/DiscoveryEngine.cs) | 45/45 | **100%** | Optimal |
| [`DeepFingerprintEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeepFingerprintEngine.cs) | 55/56 | **98.2%** | Optimal |
| [`SsdpDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/SsdpDiscoveryMethod.cs) | 130/135 | **96.3%** | Optimal |
| [`NbnsProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/NbnsProbe.cs) | 81/92 | **88%** | Optimal |
| [`ArpDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/ArpDiscoveryMethod.cs) | 106/124 | **85.5%** | Optimal |
| [`MdnsProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/MdnsProbe.cs) | 128/157 | **81.5%** | Optimal |
| [`BannerGrabProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/BannerGrabProbe.cs) | 130/163 | **79.8%** | Moderate |
| [`SnmpProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/SnmpProbe.cs) | 88/112 | **78.6%** | Moderate |
| [`SsdpProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/SsdpProbe.cs) | 64/104 | **61.5%** | Moderate |
| [`MdnsDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/MdnsDiscoveryMethod.cs) | 132/223 | **59.2%** | Moderate |
| [`DeviceClassifierEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeviceClassifierEngine.cs) | 164/297 | **55.2%** | Moderate |

#### Core / Engines

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`VulnerabilityEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/VulnerabilityEngine.cs) | 38/38 | **100%** | Optimal |
| [`ISubnetScannerDependencies.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ISubnetScannerDependencies.cs) | 12/12 | **100%** | Optimal |
| [`PortScanner.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/PortScanner.cs) | 131/132 | **99.2%** | Optimal |
| [`ConnectivityMonitor.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ConnectivityMonitor.cs) | 249/272 | **91.5%** | Optimal |
| [`WakeOnLan.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/WakeOnLan.cs) | 84/101 | **83.2%** | Optimal |
| [`ArpResolver.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ArpResolver.cs) | 140/198 | **70.7%** | Moderate |
| [`TracerouteEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/TracerouteEngine.cs) | 47/75 | **62.7%** | Moderate |
| [`IntrusionDetector.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/IntrusionDetector.cs) | 33/53 | **62.3%** | Moderate |
| [`SubnetScanner.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/SubnetScanner.cs) | 281/539 | **52.1%** | Moderate |

#### Core / Models & Messaging

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`NetworkNodeDictionaryExtensions.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/NetworkNodeDictionaryExtensions.cs) | 3/3 | **100%** | Optimal |
| [`AlertEvent.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/AlertEvent.cs) | 6/6 | **100%** | Optimal |
| [`EventAggregator.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Messaging/EventAggregator.cs) | 42/63 | **66.7%** | Moderate |
| [`NetworkNode.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/NetworkNode.cs) | 13/31 | **41.9%** | Low |

#### Core / Services

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`Logger.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/Logger.cs) | 64/87 | **73.6%** | Moderate |
| [`AppUtils.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/AppUtils.cs) | 103/155 | **66.5%** | Moderate |
| [`AudioService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/AudioService.cs) | 14/22 | **63.6%** | Moderate |
| [`ScannerDiagnostics.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/ScannerDiagnostics.cs) | 133/218 | **61%** | Moderate |
| [`IntrusionAlerter.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/IntrusionAlerter.cs) | 33/55 | **60%** | Moderate |
| [`SecurityService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/SecurityService.cs) | 9/20 | **45%** | Low |
| [`EmailService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/EmailService.cs) | 20/47 | **42.6%** | Low |

#### Data Layer

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`VendorLookup.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/VendorLookup.cs) | 3707/3707 | **100%** | Optimal |
| [`LocalDatabase.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/LocalDatabase.cs) | 361/498 | **72.5%** | Moderate |
| [`CredentialVault.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/CredentialVault.cs) | 48/125 | **38.4%** | Low |
| [`DatabaseBackupService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/DatabaseBackupService.cs) | 36/107 | **33.6%** | Low |

#### Other

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`Program.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Program.cs) | 0/58 | **0%** | Low |
| [`App.axaml.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/App.axaml.cs) | 0/19 | **0%** | Low |

#### UI / Presentation Layer

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`UptimeChartControl.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Controls/UptimeChartControl.cs) | 66/66 | **100%** | Optimal |
| [`RadarCanvas.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Controls/RadarCanvas.cs) | 40/300 | **13.3%** | Low |
| [`SystemLogsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SystemLogsPage.cs) | 0/180 | **0%** | Low |
| [`SupportPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SupportPage.cs) | 0/424 | **0%** | Low |
| [`SettingsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SettingsPage.cs) | 0/463 | **0%** | Low |
| [`ThemeTokens.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Theme/ThemeTokens.cs) | 0/278 | **0%** | Low |
| [`DarkPurpleTheme.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Theme/DarkPurpleTheme.cs) | 0/339 | **0%** | Low |
| [`TraceroutePage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/TraceroutePage.cs) | 0/123 | **0%** | Low |
| [`ScannerPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/ScannerPage.cs) | 0/654 | **0%** | Low |
| [`AlertsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/AlertsPage.cs) | 0/136 | **0%** | Low |
| [`TopNavBar.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Navigation/TopNavBar.cs) | 0/104 | **0%** | Low |
| [`SideNavBar.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Navigation/SideNavBar.cs) | 0/193 | **0%** | Low |
| [`PortScansPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/PortScansPage.cs) | 0/247 | **0%** | Low |
| [`InventoryPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/InventoryPage.cs) | 0/1077 | **0%** | Low |
| [`DashboardPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/DashboardPage.cs) | 0/130 | **0%** | Low |

