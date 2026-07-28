🎯 **What:** The testing gap in `PortScanner.ScanPortsAsync` (and related methods `ScanRangeAsync`, `ScanCommonPortsAsync`) was addressed by writing tests that setup a dummy `TcpListener` on loopback and verify the port scanner detects the open port.

📊 **Coverage:**
- `ScanPortsAsync` correctly finding an open port.
- `ScanPortsAsync` correctly timing out and returning empty when testing against a non-routable TEST-NET-1 IP (`192.0.2.1`).
- `ScanRangeAsync` successfully scanning a range of ports to detect an open loopback port.
- `ScanCommonPortsAsync` successfully checking common ports.

✨ **Result:** Test coverage for `PortScanner` has been significantly improved.
