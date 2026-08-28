# NodeRadar Pro Code Coverage & Quality Report

> Generated automatically on **2026-08-29 02:22:42 AM** via scripts/Test-Coverage.ps1.

### Solution Coverage Overview

| Metric | Coverage Value | Status |
| :--- | :---: | :---: |
| **Total Executable Lines** | **6543 / 12977** | **50.4%** |
| **Total Decision Branches** | **2354 / 5968** | **39.4%** |
| **Active Unit Tests** | **179 Passing (100%)** | Optimal |

---

### Domain Layer Breakdown

| Architectural Layer | Files | Covered Lines | Line Coverage | Branch Coverage | Health |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Core / Discovery & Fingerprinting** | 14 | 1003 / 1515 | **66.2%** | 56.9% | Moderate |
| **Core / Engines** | 9 | 776 / 1437 | **54%** | 53.2% | Moderate |
| **Core / Models & Messaging** | 4 | 62 / 99 | **62.6%** | 30.6% | Moderate |
| **Core / Services** | 8 | 466 / 732 | **63.7%** | 64% | Moderate |
| **Data Layer** | 4 | 4137 / 4419 | **93.6%** | 59% | Optimal |
| **Other** | 2 | 0 / 76 | **0%** | 0% | Attention Required |
| **UI / Presentation Layer** | 15 | 99 / 4699 | **2.1%** | 2.4% | Attention Required |

---

### Detailed Component Analysis

#### Core / Discovery & Fingerprinting

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`MacOuiProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/MacOuiProbe.cs) | 22/22 | **100%** | Optimal |
| [`FingerprintResult.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/FingerprintResult.cs) | 3/3 | **100%** | Optimal |
| [`DeviceIconMapper.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeviceIconMapper.cs) | 30/30 | **100%** | Optimal |
| [`DiscoveryEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/DiscoveryEngine.cs) | 45/45 | **100%** | Optimal |
| [`DeepFingerprintEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeepFingerprintEngine.cs) | 55/56 | **98.2%** | Optimal |
| [`SsdpProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/SsdpProbe.cs) | 82/85 | **96.5%** | Optimal |
| [`NbnsProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/NbnsProbe.cs) | 82/93 | **88.2%** | Optimal |
| [`MdnsProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/MdnsProbe.cs) | 128/157 | **81.5%** | Optimal |
| [`SnmpProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/SnmpProbe.cs) | 88/112 | **78.6%** | Moderate |
| [`BannerGrabProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/BannerGrabProbe.cs) | 117/150 | **78%** | Moderate |
| [`MdnsDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/MdnsDiscoveryMethod.cs) | 132/223 | **59.2%** | Moderate |
| [`DeviceClassifierEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeviceClassifierEngine.cs) | 164/297 | **55.2%** | Moderate |
| [`ArpDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/ArpDiscoveryMethod.cs) | 55/113 | **48.7%** | Low |
| [`SsdpDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/SsdpDiscoveryMethod.cs) | 0/129 | **0%** | Low |

#### Core / Engines

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`ISubnetScannerDependencies.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ISubnetScannerDependencies.cs) | 12/12 | **100%** | Optimal |
| [`VulnerabilityEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/VulnerabilityEngine.cs) | 38/38 | **100%** | Optimal |
| [`PortScanner.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/PortScanner.cs) | 131/132 | **99.2%** | Optimal |
| [`WakeOnLan.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/WakeOnLan.cs) | 83/101 | **82.2%** | Optimal |
| [`TracerouteEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/TracerouteEngine.cs) | 47/75 | **62.7%** | Moderate |
| [`ArpResolver.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ArpResolver.cs) | 118/232 | **50.9%** | Moderate |
| [`SubnetScanner.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/SubnetScanner.cs) | 260/527 | **49.3%** | Low |
| [`IntrusionDetector.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/IntrusionDetector.cs) | 25/53 | **47.2%** | Low |
| [`ConnectivityMonitor.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ConnectivityMonitor.cs) | 62/267 | **23.2%** | Low |

#### Core / Models & Messaging

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`NetworkNodeDictionaryExtensions.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/NetworkNodeDictionaryExtensions.cs) | 3/3 | **100%** | Optimal |
| [`EventAggregator.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Messaging/EventAggregator.cs) | 46/59 | **78%** | Moderate |
| [`NetworkNode.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/NetworkNode.cs) | 13/31 | **41.9%** | Low |
| [`AlertEvent.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/AlertEvent.cs) | 0/6 | **0%** | Low |

#### Core / Services

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`AppUtils.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/AppUtils.cs) | 93/121 | **76.9%** | Moderate |
| [`Logger.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/Logger.cs) | 64/87 | **73.6%** | Moderate |
| [`AudioService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/AudioService.cs) | 14/22 | **63.6%** | Moderate |
| [`UpdateService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/UpdateService.cs) | 100/162 | **61.7%** | Moderate |
| [`ScannerDiagnostics.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/ScannerDiagnostics.cs) | 133/218 | **61%** | Moderate |
| [`IntrusionAlerter.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/IntrusionAlerter.cs) | 33/55 | **60%** | Moderate |
| [`SecurityService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/SecurityService.cs) | 9/20 | **45%** | Low |
| [`EmailService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/EmailService.cs) | 20/47 | **42.6%** | Low |

#### Data Layer

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`VendorLookup.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/VendorLookup.cs) | 3707/3707 | **100%** | Optimal |
| [`LocalDatabase.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/LocalDatabase.cs) | 346/480 | **72.1%** | Moderate |
| [`CredentialVault.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/CredentialVault.cs) | 48/125 | **38.4%** | Low |
| [`DatabaseBackupService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/DatabaseBackupService.cs) | 36/107 | **33.6%** | Low |

#### Other

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`Program.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Program.cs) | 0/57 | **0%** | Low |
| [`App.axaml.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/App.axaml.cs) | 0/19 | **0%** | Low |

#### UI / Presentation Layer

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`UptimeChartControl.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Controls/UptimeChartControl.cs) | 62/62 | **100%** | Optimal |
| [`RadarCanvas.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Controls/RadarCanvas.cs) | 37/242 | **15.3%** | Low |
| [`SystemLogsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SystemLogsPage.cs) | 0/159 | **0%** | Low |
| [`SupportPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SupportPage.cs) | 0/415 | **0%** | Low |
| [`SettingsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SettingsPage.cs) | 0/490 | **0%** | Low |
| [`ThemeTokens.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Theme/ThemeTokens.cs) | 0/261 | **0%** | Low |
| [`DarkPurpleTheme.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Theme/DarkPurpleTheme.cs) | 0/424 | **0%** | Low |
| [`TraceroutePage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/TraceroutePage.cs) | 0/123 | **0%** | Low |
| [`ScannerPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/ScannerPage.cs) | 0/655 | **0%** | Low |
| [`AlertsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/AlertsPage.cs) | 0/136 | **0%** | Low |
| [`TopNavBar.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Navigation/TopNavBar.cs) | 0/104 | **0%** | Low |
| [`SideNavBar.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Navigation/SideNavBar.cs) | 0/193 | **0%** | Low |
| [`PortScansPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/PortScansPage.cs) | 0/247 | **0%** | Low |
| [`InventoryPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/InventoryPage.cs) | 0/1058 | **0%** | Low |
| [`DashboardPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/DashboardPage.cs) | 0/130 | **0%** | Low |

