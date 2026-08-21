# NodeRadar Pro Code Coverage & Quality Report

> Generated automatically on **2026-08-22 02:54:48 AM** via scripts/Test-Coverage.ps1.

### Solution Coverage Overview

| Metric | Coverage Value | Status |
| :--- | :---: | :---: |
| **Total Executable Lines** | **5188 / 12796** | **40.5%** |
| **Total Decision Branches** | **1596 / 13080** | **12.2%** |
| **Active Unit Tests** | **179 Passing (100%)** | Optimal |

---

### Domain Layer Breakdown

| Architectural Layer | Files | Covered Lines | Line Coverage | Branch Coverage | Health |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Core / Discovery & Fingerprinting** | 16 | 347 / 1567 | **22.1%** | 16.5% | Attention Required |
| **Core / Engines** | 9 | 552 / 1359 | **40.6%** | 21.9% | Attention Required |
| **Core / Models & Messaging** | 8 | 138 / 178 | **77.5%** | 14.5% | Moderate |
| **Core / Services** | 8 | 186 / 584 | **31.8%** | 17.5% | Attention Required |
| **Data Layer** | 3 | 3965 / 4219 | **94%** | 26.6% | Optimal |
| **Other** | 3 | 0 / 76 | **0%** | 0% | Attention Required |
| **UI / Presentation Layer** | 15 | 0 / 4813 | **0%** | 0% | Attention Required |

---

### Detailed Component Analysis

#### Core / Discovery & Fingerprinting

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`DeviceIconMapper.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeviceIconMapper.cs) | 30/30 | **100%** | Optimal |
| [`FingerprintResult.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/FingerprintResult.cs) | 12/12 | **100%** | Optimal |
| [`DeviceClassifierEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeviceClassifierEngine.cs) | 157/292 | **53.8%** | Moderate |
| [`BannerGrabProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/BannerGrabProbe.cs) | 68/150 | **45.3%** | Low |
| [`DeepFingerprintEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/DeepFingerprintEngine.cs) | 19/47 | **40.4%** | Low |
| [`SsdpProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/SsdpProbe.cs) | 21/84 | **25%** | Low |
| [`MdnsProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/MdnsProbe.cs) | 40/206 | **19.4%** | Low |
| [`MacOuiProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/MacOuiProbe.cs) | 0/28 | **0%** | Low |
| [`NbnsProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/NbnsProbe.cs) | 0/93 | **0%** | Low |
| [`ArpDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/ArpDiscoveryMethod.cs) | 0/109 | **0%** | Low |
| [`SnmpProbe.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Fingerprinting/Probes/SnmpProbe.cs) | 0/76 | **0%** | Low |
| [`NetworkDevice.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/NetworkDevice.cs) | 0/8 | **0%** | Low |
| [`SsdpDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/SsdpDiscoveryMethod.cs) | 0/129 | **0%** | Low |
| [`MdnsDiscoveryMethod.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/MdnsDiscoveryMethod.cs) | 0/216 | **0%** | Low |
| [`DiagnosticLogger.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/DiagnosticLogger.cs) | 0/3 | **0%** | Low |
| [`DiscoveryEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Discovery/DiscoveryEngine.cs) | 0/84 | **0%** | Low |

#### Core / Engines

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`VulnerabilityEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/VulnerabilityEngine.cs) | 38/38 | **100%** | Optimal |
| [`PortScanner.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/PortScanner.cs) | 98/99 | **99%** | Optimal |
| [`WakeOnLan.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/WakeOnLan.cs) | 80/90 | **88.9%** | Optimal |
| [`TracerouteEngine.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/TracerouteEngine.cs) | 52/80 | **65%** | Moderate |
| [`ArpResolver.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ArpResolver.cs) | 118/187 | **63.1%** | Moderate |
| [`ConnectivityMonitor.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ConnectivityMonitor.cs) | 63/280 | **22.5%** | Low |
| [`SubnetScanner.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/SubnetScanner.cs) | 103/541 | **19%** | Low |
| [`ISubnetScannerDependencies.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/ISubnetScannerDependencies.cs) | 0/14 | **0%** | Low |
| [`IntrusionDetector.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Engines/IntrusionDetector.cs) | 0/30 | **0%** | Low |

#### Core / Models & Messaging

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`UptimeSnapshot.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/UptimeSnapshot.cs) | 5/5 | **100%** | Optimal |
| [`LogEntry.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/LogEntry.cs) | 6/6 | **100%** | Optimal |
| [`NetworkNodeDictionaryExtensions.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/NetworkNodeDictionaryExtensions.cs) | 3/3 | **100%** | Optimal |
| [`AppSettings.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/AppSettings.cs) | 28/28 | **100%** | Optimal |
| [`EventAggregator.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Messaging/EventAggregator.cs) | 46/59 | **78%** | Moderate |
| [`NetworkNode.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/NetworkNode.cs) | 41/59 | **69.5%** | Moderate |
| [`AlertEvent.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Models/AlertEvent.cs) | 9/15 | **60%** | Moderate |
| [`Messages.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Messaging/Messages.cs) | 0/3 | **0%** | Low |

#### Core / Services

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`SecurityService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/SecurityService.cs) | 16/16 | **100%** | Optimal |
| [`Logger.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/Logger.cs) | 57/70 | **81.4%** | Optimal |
| [`AudioService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/AudioService.cs) | 15/23 | **65.2%** | Moderate |
| [`IntrusionAlerter.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/IntrusionAlerter.cs) | 35/57 | **61.4%** | Moderate |
| [`EmailService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/EmailService.cs) | 19/46 | **41.3%** | Low |
| [`UpdateService.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/UpdateService.cs) | 44/114 | **38.6%** | Low |
| [`AppUtils.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/AppUtils.cs) | 0/40 | **0%** | Low |
| [`ScannerDiagnostics.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Core/Services/ScannerDiagnostics.cs) | 0/218 | **0%** | Low |

#### Data Layer

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`VendorLookup.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/VendorLookup.cs) | 3706/3706 | **100%** | Optimal |
| [`LocalDatabase.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/LocalDatabase.cs) | 259/482 | **53.7%** | Moderate |
| [`OuiDatabase.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Data/OuiDatabase.cs) | 0/31 | **0%** | Low |

#### Other

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`Program.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/Program.cs) | 0/54 | **0%** | Low |
| [`App.axaml.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/App.axaml.cs) | 0/19 | **0%** | Low |
| [`App.axaml`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/App.axaml) | 0/3 | **0%** | Low |

#### UI / Presentation Layer

| Source Component | Lines | Coverage | Status |
| :--- | :---: | :---: | :---: |
| [`SupportPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SupportPage.cs) | 0/412 | **0%** | Low |
| [`SettingsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SettingsPage.cs) | 0/518 | **0%** | Low |
| [`ScannerPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/ScannerPage.cs) | 0/740 | **0%** | Low |
| [`SystemLogsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/SystemLogsPage.cs) | 0/156 | **0%** | Low |
| [`ThemeTokens.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Theme/ThemeTokens.cs) | 0/261 | **0%** | Low |
| [`DarkPurpleTheme.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Theme/DarkPurpleTheme.cs) | 0/416 | **0%** | Low |
| [`TraceroutePage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/TraceroutePage.cs) | 0/123 | **0%** | Low |
| [`PortScansPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/PortScansPage.cs) | 0/253 | **0%** | Low |
| [`SideNavBar.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Navigation/SideNavBar.cs) | 0/193 | **0%** | Low |
| [`UptimeChartControl.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Controls/UptimeChartControl.cs) | 0/62 | **0%** | Low |
| [`RadarCanvas.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Controls/RadarCanvas.cs) | 0/242 | **0%** | Low |
| [`TopNavBar.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Navigation/TopNavBar.cs) | 0/104 | **0%** | Low |
| [`InventoryPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/InventoryPage.cs) | 0/1063 | **0%** | Low |
| [`DashboardPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/DashboardPage.cs) | 0/133 | **0%** | Low |
| [`AlertsPage.cs`](file:///C:/Users/tryku/Desktop/Coding/Projects/C#/Cross-Platform/NodeRadar Pro/UI/Pages/AlertsPage.cs) | 0/137 | **0%** | Low |

