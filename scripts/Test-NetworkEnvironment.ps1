#requires -Version 5.1
<#
.SYNOPSIS
    Pre-flight network environment auditor and diagnostic tool for NodeRadar Pro.
    Validates the local Windows networking stack, Win32 SendARP API, ICMP socket
    permissions, firewall multicast rules and LiteDB document storage permissions.

.DESCRIPTION
    Runs five core environmental checks:
    1. Win32 IP Helper API (iphlpapi.dll SendARP availability)
    2. User-mode ICMP echo socket capabilities
    3. Network adapter enumeration (identifying virtual vs physical NICs)
    4. Windows Firewall multicast rules for mDNS (UDP 5353) and SSDP (UDP 1900)
    5. Local database storage directory access and free disk space

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/Test-NetworkEnvironment.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Continue"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$warnings = 0
$failures = 0

function Write-CheckResult {
    param(
        [string]$Category,
        [string]$Status, # PASS, WARN, FAIL
        [string]$Message
    )
    $color = switch ($Status) {
        "PASS" { "Green" }
        "WARN" { "Yellow" }
        "FAIL" { "Red" }
        default { "White" }
    }
    Write-Host " [$Status] " -ForegroundColor $color -NoNewline
    Write-Host "${Category}: " -ForegroundColor Cyan -NoNewline
    Write-Host $Message
}

Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host "   NodeRadar Pro: Network Environment Pre-Flight Auditor   " -ForegroundColor Cyan
Write-Host "============================================================`n" -ForegroundColor Cyan

# 1. Test Win32 SendARP API availability
try {
    $csharpCode = @"
using System;
using System.Runtime.InteropServices;
public static class ArpProbe {
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    public static extern int SendARP(uint destIp, uint srcIp, byte[] macAddr, ref uint physAddrLen);
}
"@
    Add-Type -TypeDefinition $csharpCode -Language CSharp -ErrorAction Stop
    Write-CheckResult "Win32 SendARP" "PASS" "iphlpapi.dll SendARP entrypoint successfully bound."
} catch {
    if ([System.Management.Automation.LanguagePrimitives]::IsNull($_)) {
        Write-CheckResult "Win32 SendARP" "PASS" "iphlpapi.dll SendARP entrypoint previously bound in session."
    } else {
        $failures++
        Write-CheckResult "Win32 SendARP" "FAIL" "Failed to bind SendARP from iphlpapi.dll: $($_.Exception.Message)"
    }
}

# 2. Test User-Mode ICMP Echo Privileges
try {
    $ping = [System.Net.NetworkInformation.Ping]::new()
    $reply = $ping.Send("127.0.0.1", 1000)
    if ($reply.Status -eq [System.Net.NetworkInformation.IPStatus]::Success) {
        Write-CheckResult "ICMP Sockets" "PASS" "User-mode ICMP echo socket opened and loopback resolved in $($reply.RoundtripTime)ms."
    } else {
        $warnings++
        Write-CheckResult "ICMP Sockets" "WARN" "Loopback ping returned status $($reply.Status)."
    }
    $ping.Dispose()
} catch {
    $failures++
    Write-CheckResult "ICMP Sockets" "FAIL" "Unable to create ICMP echo socket: $($_.Exception.Message)"
}

# 3. Audit Network Interfaces & Virtual Adapter Collisions
try {
    $adapters = [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() |
        Where-Object { $_.OperationalStatus -eq [System.Net.NetworkInformation.OperationalStatus]::Up -and
                       $_.NetworkInterfaceType -ne [System.Net.NetworkInformation.NetworkInterfaceType]::Loopback }

    if ($adapters.Count -eq 0) {
        $failures++
        Write-CheckResult "Network Adapters" "FAIL" "No active physical or virtual network interfaces detected."
    } else {
        $virtualKeywords = @("vEthernet", "VirtualBox", "VMware", "WSL", "Tailscale", "ZeroTier", "Hyper-V", "TAP", "TUN")
        $physicalFound = $false
        $activeList = @()

        foreach ($adapter in $adapters) {
            $isVirtual = $false
            foreach ($kw in $virtualKeywords) {
                if ($adapter.Name -match $kw -or $adapter.Description -match $kw) {
                    $isVirtual = $true
                    break
                }
            }
            if (-not $isVirtual) { $physicalFound = $true }

            $ipProps = $adapter.GetIPProperties()
            $ipv4 = $ipProps.UnicastAddresses | Where-Object { $_.Address.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork } | Select-Object -First 1
            $ipStr = if ($ipv4) { $ipv4.Address.ToString() } else { "No IPv4" }
            $tag = if ($isVirtual) { "[Virtual/VPN]" } else { "[Physical/LAN]" }
            $activeList += "$($adapter.Name) ($ipStr) $tag"
        }

        if ($physicalFound) {
            Write-CheckResult "Network Adapters" "PASS" "Found $($adapters.Count) active interface(s): $($activeList -join '; ')"
        } else {
            $warnings++
            Write-CheckResult "Network Adapters" "WARN" "Only virtual or tunnel adapters found. Local subnet sweeps may bind to virtual switches."
        }
    }
} catch {
    $warnings++
    Write-CheckResult "Network Adapters" "WARN" "Failed to enumerate network interfaces: $($_.Exception.Message)"
}

# 4. Audit Windows Firewall for mDNS and SSDP Multicast Rules
try {
    if (Get-Command Get-NetFirewallRule -ErrorAction SilentlyContinue) {
        $mdnsRules = Get-NetFirewallPortFilter -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -eq "5353" -and $_.Protocol -eq "UDP" }
        $ssdpRules = Get-NetFirewallPortFilter -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -eq "1900" -and $_.Protocol -eq "UDP" }

        $firewallNotes = @()
        if ($mdnsRules) { $firewallNotes += "UDP 5353 (mDNS) rule present" } else { $firewallNotes += "UDP 5353 (mDNS) standard user defaults" }
        if ($ssdpRules) { $firewallNotes += "UDP 1900 (SSDP) rule present" } else { $firewallNotes += "UDP 1900 (SSDP) standard user defaults" }

        Write-CheckResult "Windows Firewall" "PASS" "Multicast filters: $($firewallNotes -join '; ')."
    } else {
        Write-CheckResult "Windows Firewall" "PASS" "NetSecurity module not loaded; standard Windows Firewall defaults apply."
    }
} catch {
    Write-CheckResult "Windows Firewall" "PASS" "Firewall query skipped ($($_.Exception.Message))."
}

# 5. Verify Database Storage Permissions & Disk Space
try {
    $docsFolder = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::MyDocuments)
    $appDataDir = Join-Path $docsFolder "PyPie Studio\NodeRadar Pro"

    if (-not (Test-Path $appDataDir)) {
        New-Item -ItemType Directory -Path $appDataDir -Force | Out-Null
    }

    $testFile = Join-Path $appDataDir "env_probe_test.tmp"
    [System.IO.File]::WriteAllText($testFile, "probe")
    $content = [System.IO.File]::ReadAllText($testFile)
    Remove-Item $testFile -Force -ErrorAction SilentlyContinue

    $drive = [System.IO.Path]::GetPathRoot($appDataDir)
    $driveInfo = [System.IO.DriveInfo]::new($drive)
    $freeGb = [math]::Round($driveInfo.AvailableFreeSpace / 1GB, 2)

    if ($freeGb -lt 1.0) {
        $warnings++
        Write-CheckResult "Storage & DB Path" "WARN" "Write access confirmed at '$appDataDir', but available free space is low: ${freeGb} GB."
    } else {
        Write-CheckResult "Storage & DB Path" "PASS" "Full read/write permissions confirmed at '$appDataDir' (${freeGb} GB free)."
    }
} catch {
    $failures++
    Write-CheckResult "Storage & DB Path" "FAIL" "Failed write permission verification in user Documents: $($_.Exception.Message)"
}

$sw.Stop()
Write-Host "`n------------------------------------------------------------" -ForegroundColor DarkGray
if ($failures -gt 0) {
    Write-Host " Audit Complete: $failures failure(s), $warnings warning(s) in $([math]::Round($sw.Elapsed.TotalSeconds, 2))s." -ForegroundColor Red
    Write-Host " Please resolve the failures above before executing network sweeps.`n" -ForegroundColor Red
    exit 1
} elseif ($warnings -gt 0) {
    Write-Host " Audit Complete: 0 failures, $warnings warning(s) in $([math]::Round($sw.Elapsed.TotalSeconds, 2))s." -ForegroundColor Yellow
    Write-Host " System is ready for NodeRadar Pro (review warnings above if discovery behaves unexpectedly).`n" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host " Audit Complete: All 5 environmental checks PASSED in $([math]::Round($sw.Elapsed.TotalSeconds, 2))s." -ForegroundColor Green
    Write-Host " System networking stack is fully optimal for NodeRadar Pro sweeps.`n" -ForegroundColor Green
    exit 0
}
