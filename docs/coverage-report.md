# NodeRadar Pro Code Coverage & Quality Report

> Generated automatically on **2026-10-28 11:03:22 PM** via scripts/Test-Coverage.ps1.

### Solution Coverage Overview

| Metric | Coverage Value | Status |
| :--- | :---: | :---: |
| **Total Executable Lines** | **11036 / 15216** | **72.5%** |
| **Total Decision Branches** | **4250 / 7212** | **58.9%** |
| **Active Unit Tests** | **698 Passing (100%)** | Optimal |

---

### Domain Layer Breakdown

| Architectural Layer | Files | Covered Lines | Line Coverage | Branch Coverage | Health |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Core / Discovery & Fingerprinting** | 15 | 1442 / 1720 | **83.8%** | 73.9% | Optimal |
| **Core / Engines** | 9 | 1329 / 1770 | **75.1%** | 68.2% | Moderate |
| **Core / Models & Messaging** | 4 | 110 / 122 | **90.2%** | 68.4% | Optimal |
| **Core / Services** | 9 | 634 / 940 | **67.4%** | 70.1% | Moderate |
| **Data Layer** | 4 | 4663 / 4789 | **97.4%** | 77.1% | Optimal |
| **Other** | 2 | 0 / 112 | **0%** | 0% | Attention Required |
| **UI / Presentation Layer** | 15 | 2858 / 5763 | **49.6%** | 30.1% | Attention Required |

---

### Detailed Component Analysis

#### Core / Discovery & Fingerprinting

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`FingerprintResult.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/FingerprintResult.cs) | 3/3 | **100%** | Optimal |
| [`MacOuiProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/MacOuiProbe.cs) | 22/22 | **100%** | Optimal |
| [`DeviceIconMapper.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeviceIconMapper.cs) | 30/30 | **100%** | Optimal |
| [`DeepFingerprintEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeepFingerprintEngine.cs) | 55/56 | **98.2%** | Optimal |
| [`NbnsProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/NbnsProbe.cs) | 89/92 | **96.7%** | Optimal |
| [`MdnsProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/MdnsProbe.cs) | 192/201 | **95.5%** | Optimal |
| [`DeviceClassifierEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeviceClassifierEngine.cs) | 273/308 | **88.6%** | Optimal |
| [`DiscoveryEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/DiscoveryEngine.cs) | 63/72 | **87.5%** | Optimal |
| [`ArpDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/ArpDiscoveryMethod.cs) | 106/124 | **85.5%** | Optimal |
| [`SsdpDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/SsdpDiscoveryMethod.cs) | 126/148 | **85.1%** | Optimal |
| [`MdnsDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/MdnsDiscoveryMethod.cs) | 169/210 | **80.5%** | Optimal |
| [`SnmpProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/SnmpProbe.cs) | 90/114 | **78.9%** | Moderate |
| [`SsdpProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/SsdpProbe.cs) | 107/137 | **78.1%** | Moderate |
| [`DefaultMulticastSocket.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/DefaultMulticastSocket.cs) | 24/31 | **77.4%** | Moderate |
| [`BannerGrabProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/BannerGrabProbe.cs) | 93/172 | **54.1%** | Moderate |

#### Core / Engines

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`VulnerabilityEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/VulnerabilityEngine.cs) | 38/38 | **100%** | Optimal |
| [`PortScanner.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/PortScanner.cs) | 130/132 | **98.5%** | Optimal |
| [`WakeOnLan.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/WakeOnLan.cs) | 94/108 | **87%** | Optimal |
| [`IntrusionDetector.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/IntrusionDetector.cs) | 62/81 | **76.5%** | Moderate |
| [`ArpResolver.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ArpResolver.cs) | 207/277 | **74.7%** | Moderate |
| [`SubnetScanner.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/SubnetScanner.cs) | 474/652 | **72.7%** | Moderate |
| [`ConnectivityMonitor.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ConnectivityMonitor.cs) | 251/358 | **70.1%** | Moderate |
| [`TracerouteEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/TracerouteEngine.cs) | 54/80 | **67.5%** | Moderate |
| [`ISubnetScannerDependencies.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ISubnetScannerDependencies.cs) | 19/44 | **43.2%** | Low |

#### Core / Models & Messaging

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`NetworkNodeDictionaryExtensions.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/NetworkNodeDictionaryExtensions.cs) | 3/3 | **100%** | Optimal |
| [`AlertEvent.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/AlertEvent.cs) | 11/11 | **100%** | Optimal |
| [`EventAggregator.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Messaging/EventAggregator.cs) | 63/63 | **100%** | Optimal |
| [`NetworkNode.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/NetworkNode.cs) | 33/45 | **73.3%** | Moderate |

#### Core / Services

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`CsvExportService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/CsvExportService.cs) | 58/59 | **98.3%** | Optimal |
| [`EmailService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/EmailService.cs) | 73/78 | **93.6%** | Optimal |
| [`SecurityService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/SecurityService.cs) | 47/58 | **81%** | Optimal |
| [`Logger.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/Logger.cs) | 77/101 | **76.2%** | Moderate |
| [`ScannerDiagnostics.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/ScannerDiagnostics.cs) | 147/218 | **67.4%** | Moderate |
| [`AppUtils.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/AppUtils.cs) | 107/180 | **59.4%** | Moderate |
| [`IntrusionAlerter.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/IntrusionAlerter.cs) | 41/77 | **53.2%** | Moderate |
| [`AudioService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/AudioService.cs) | 21/40 | **52.5%** | Moderate |
| [`ChangelogService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/ChangelogService.cs) | 63/129 | **48.8%** | Low |

#### Data Layer

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`VendorLookup.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/VendorLookup.cs) | 3760/3760 | **100%** | Optimal |
| [`CredentialVault.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/CredentialVault.cs) | 128/139 | **92.1%** | Optimal |
| [`LocalDatabase.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/LocalDatabase.cs) | 649/721 | **90%** | Optimal |
| [`DatabaseBackupService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/DatabaseBackupService.cs) | 126/169 | **74.6%** | Moderate |

#### Other

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`Program.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Program.cs) | 0/93 | **0%** | Low |
| [`App.axaml.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/App.axaml.cs) | 0/19 | **0%** | Low |

#### UI / Presentation Layer

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`UptimeChartControl.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Controls/UptimeChartControl.cs) | 66/66 | **100%** | Optimal |
| [`DashboardPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/DashboardPage.cs) | 180/185 | **97.3%** | Optimal |
| [`AlertsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/AlertsPage.cs) | 278/289 | **96.2%** | Optimal |
| [`ThemeTokens.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Theme/ThemeTokens.cs) | 307/331 | **92.7%** | Optimal |
| [`SettingsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SettingsPage.cs) | 490/625 | **78.4%** | Moderate |
| [`RadarCanvas.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Controls/RadarCanvas.cs) | 280/376 | **74.5%** | Moderate |
| [`InventoryPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/InventoryPage.cs) | 766/1244 | **61.6%** | Moderate |
| [`ScannerPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/ScannerPage.cs) | 410/686 | **59.8%** | Moderate |
| [`SupportPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SupportPage.cs) | 74/492 | **15%** | Low |
| [`SystemLogsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SystemLogsPage.cs) | 7/337 | **2.1%** | Low |
| [`DarkPurpleTheme.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Theme/DarkPurpleTheme.cs) | 0/406 | **0%** | Low |
| [`PortScansPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/PortScansPage.cs) | 0/262 | **0%** | Low |
| [`TraceroutePage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/TraceroutePage.cs) | 0/150 | **0%** | Low |
| [`TopNavBar.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Navigation/TopNavBar.cs) | 0/104 | **0%** | Low |
| [`SideNavBar.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Navigation/SideNavBar.cs) | 0/210 | **0%** | Low |

