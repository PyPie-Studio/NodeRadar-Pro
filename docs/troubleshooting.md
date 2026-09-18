# NodeRadar Pro Troubleshooting Guide

Field diagnostic guide for resolving network topology anomalies, Windows firewall blocks, virtual adapter precedence and multicast discovery limitations.

---

## 1. Multi-NIC & Virtual Adapter Precedence

### Symptom
Subnet sweeps target an unexpected IP range (e.g. `172.x.x.x` or `192.168.56.x` instead of your physical LAN `192.168.1.x`).

### Cause
Virtualization software (WSL2, Hyper-V, VMware Workstation, VirtualBox) and VPN adapters install virtual network adapters with high interface metrics or priority.

### Diagnosis
Run PowerShell to inspect active adapters and route metrics:
```powershell
Get-NetRoute -DestinationPrefix "0.0.0.0/0" | Sort-Object RouteMetric | Select-Object InterfaceAlias, NextHop, RouteMetric
```

### Remediation
1. Verify the selected adapter in NodeRadar Pro settings matches your active physical interface.
2. Run the network environment pre-flight script:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NetworkEnvironment.ps1
   ```
3. To lower the route metric of your physical adapter permanently:
   ```powershell
   Set-NetIPInterface -InterfaceAlias "Ethernet" -InterfaceMetric 10
   ```

---

## 2. Hosts Visible in ARP but Dropping ICMP Pings

### Symptom
Hosts appear in the subnet scan table with valid MAC addresses and vendor names, but show status as offline or show 0 ms latency with failed ICMP probes.

### Cause
Windows Defender Firewall and macOS / Linux endpoint firewalls block inbound ICMPv4 Echo Requests (`ping`) by default on Public and Domain network profiles. However, Layer 2 Address Resolution Protocol (`ARP`) cannot be blocked by host firewalls without disconnecting the host from the network.

### Diagnosis
NodeRadar Pro uses Win32 `SendARP` as a primary link-layer discovery mechanism. If a MAC address resolves, the host is physically online on the local broadcast domain.

### Remediation
To allow ICMP echo responses on target Windows machines:
```powershell
# Enable ICMPv4 inbound rule on private profiles
netsh advfirewall firewall add rule name="Allow ICMPv4-In" protocol=icmpv4:8,any dir=in action=allow profile=private
```

---

## 3. Multicast Discovery (mDNS & SSDP) Returns No Devices

### Symptom
Deep fingerprinting fails to discover hostnames (`.local`), Apple AirPlay, Chromecast or UPnP/SSDP device profiles.

### Causes
1. **Wi-Fi Client Isolation (AP Isolation):** Many consumer and enterprise wireless access points block wireless clients from sending broadcast or multicast traffic to each other.
2. **IGMP Snooping:** Managed switches without an active IGMP querier may drop multicast packets on `224.0.0.251` (mDNS) or `239.255.255.250` (SSDP).
3. **Local Firewall Multicast Filters:** Host firewalls blocking outbound UDP socket broadcast or inbound response ports.

### Remediation
1. Disable **AP Isolation** / **Guest Network Mode** on your wireless router.
2. Verify UDP ports `5353` (mDNS) and `1900` (SSDP) are not blocked by third-party antivirus suites.
3. Test local multicast reachability with PowerShell:
   ```powershell
   Test-NetConnection -ComputerName 224.0.0.251 -Port 5353 -InformationLevel Detailed
   ```

---

## 4. Wi-Fi Power Saving Mode Latency Spikes

### Symptom
Continuous latency monitor displays periodic latency spikes (100 ms to 300 ms) every few seconds, despite no active network load.

### Cause
802.11 DTIM (Delivery Traffic Indication Message) power-save mechanisms put wireless client chips into low-power sleep states between beacon intervals. The initial probe packet wakes the radio, introducing 50 ms to 200 ms wake latency.

### Remediation
1. Set the Wi-Fi adapter power plan to **Maximum Performance** in Windows Power Options:
   ```powershell
   powercfg -setdcvalueindex SCHEME_CURRENT SUB_ENERGYSAVER 12b591b6-1930-4e33-911e-b8d9600e0000 0
   ```
2. NodeRadar Pro automatically calculates jitter using standard deviation over a sliding window, preventing isolated DTIM wake spikes from skewing baseline health metrics.

---

## 5. VPN Split-Tunneling Conflicts

### Symptom
Starting a VPN disconnects local network sweeps or produces `SocketException: Network is unreachable`.

### Cause
Full-tunnel VPN clients redirect default routes (`0.0.0.0/0`) through the virtual TUN/TAP adapter and block local LAN traffic (LAN isolation).

### Remediation
1. Configure your VPN client to enable **Allow local network access (Split Tunneling)**.
2. Check if the VPN subnet collides with your local LAN subnet (e.g. both using `192.168.1.0/24`). If they collide, reconfigure the local DHCP scope to an alternate private range (`10.10.x.0/24` or `172.16.x.0/24`).

---

## 6. Diagnostic Script Reference

Run the integrated pre-flight diagnostic script for an automated inspection of network adapters, gateways, DNS servers, ARP table entries and Windows Defender Firewall states:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NetworkEnvironment.ps1
```
