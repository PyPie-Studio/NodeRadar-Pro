#requires -Version 5.1
<#
.SYNOPSIS
    Downloads the official IEEE / Wireshark MAC manufacturer registry and updates the VendorLookup database.

.DESCRIPTION
    Fetches the live `manuf` hardware database from Wireshark / IEEE, normalizes vendor branding,
    and updates `Data/VendorLookup.cs` with high-density OUI mappings.

.PARAMETER SourceUrl
    The remote URL to download the manuf registry from (defaults to Wireshark automated endpoint).

.PARAMETER ExportJson
    When specified, also exports the parsed dataset as a JSON file.

.PARAMETER JsonPath
    The target output path for the JSON export (defaults to Data/oui_database.json).

.PARAMETER Verify
    When specified, formats code and runs the test suite and Master Gate after updating.

.EXAMPLE
    # Fetch and update VendorLookup.cs:
    powershell -ExecutionPolicy Bypass -File scripts/Update-OuiDatabase.ps1

    # Update and verify Master Gate:
    powershell -ExecutionPolicy Bypass -File scripts/Update-OuiDatabase.ps1 -Verify
#>
[CmdletBinding()]
param(
    [string]$SourceUrl = "https://www.wireshark.org/download/automated/data/manuf",
    [switch]$ExportJson,
    [string]$JsonPath = "Data/oui_database.json",
    [switch]$Verify
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$vendorLookupFile = Join-Path $root "src\NodeRadarPro\Data\VendorLookup.cs"
$jsonExportFile = Join-Path $root $JsonPath

Write-Host "== NodeRadar Pro IEEE MAC OUI Auto-Updater ==" -ForegroundColor Cyan

# 1. Fetch live manuf registry
Write-Host "Downloading manufacturer registry from $SourceUrl..." -ForegroundColor Yellow

try {
    $rawText = Invoke-RestMethod -Uri $SourceUrl -UserAgent "Mozilla/5.0 (Windows NT 10.0; Win64; x64) NodeRadarPro-OUI/1.0" -TimeoutSec 15
}
catch {
    Write-Host "Failed to download from primary source. Attempting backup mirror..." -ForegroundColor DarkYellow
    $backupUrl = "https://gitlab.com/wireshark/wireshark/-/raw/master/manuf"
    try {
        $rawText = Invoke-RestMethod -Uri $backupUrl -UserAgent "Mozilla/5.0 (Windows NT 10.0; Win64; x64) NodeRadarPro-OUI/1.0" -TimeoutSec 15
    }
    catch {
        Write-Host "Download failed: $_" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Downloaded $([math]::Round($rawText.Length / 1024, 1)) KB of OUI data." -ForegroundColor Green

# 2. Parse OUI entries
Write-Host "Parsing and normalizing hardware vendor entries..." -ForegroundColor Yellow

function Normalize-VendorName([string]$shortName, [string]$fullName) {
    $name = if ($shortName) { $shortName } else { $fullName }
    if (-not $name) { return "Unknown" }
    
    $clean = $name.Trim()
    
    # Common Brand Normalizations
    if ($clean -match "^(Apple|APPLE)") { return "Apple" }
    if ($clean -match "^(Samsung|SAMSUNG)") { return "Samsung" }
    if ($clean -match "^(Cisco|CISCO)") { return "Cisco" }
    if ($clean -match "^(Intel|INTEL)") { return "Intel" }
    if ($clean -match "^(TP-Link|TP-LINK|Tp-Link|TPLink)") { return "TP-Link" }
    if ($clean -match "^(Huawei|HUAWEI)") { return "Huawei" }
    if ($clean -match "^(Xiaomi|XIAOMI)") { return "Xiaomi" }
    if ($clean -match "^(Espressif|ESPRESSIF)") { return "Espressif (IoT)" }
    if ($clean -match "^(Raspberry|RASPBERRY)") { return "Raspberry Pi Foundation" }
    if ($clean -match "^(MikroTik|Mikrotik|MIKROTIK)") { return "MikroTik" }
    if ($clean -match "^(Ubiquiti|UBIQUITI)") { return "Ubiquiti" }
    if ($clean -match "^(Amazon|AMAZON)") { return "Amazon" }
    if ($clean -match "^(Google|GOOGLE)") { return "Google" }
    if ($clean -match "^(Sony|SONY)") { return "Sony" }
    if ($clean -match "^(Microsoft|MICROSOFT)") { return "Microsoft" }
    if ($clean -match "^(Dell|DELL)") { return "Dell" }
    if ($clean -match "^(HP|Hewlett|HEWLETT)") { return "HP" }
    if ($clean -match "^(Lenovo|LENOVO)") { return "Lenovo" }
    if ($clean -match "^(ASUS|Asus|ASUSTek)") { return "ASUS" }
    if ($clean -match "^(Netgear|NETGEAR)") { return "Netgear" }
    if ($clean -match "^(D-Link|D-LINK|Dlink)") { return "D-Link" }
    if ($clean -match "^(Hikvision|HIKVISION)") { return "Hikvision" }
    if ($clean -match "^(Dahua|DAHUA)") { return "Dahua" }
    if ($clean -match "^(Sonos|SONOS)") { return "Sonos" }
    if ($clean -match "^(Roku|ROKU)") { return "Roku" }
    if ($clean -match "^(Realtek|REALTEK)") { return "Realtek" }
    if ($clean -match "^(Broadcom|BROADCOM)") { return "Broadcom" }
    if ($clean -match "^(Giga-Byte|GIGABYTE|GigaByte)") { return "GIGABYTE" }
    if ($clean -match "^(ASRock|ASROCK)") { return "ASRock" }
    if ($clean -match "^(Micro-Star|MSI)") { return "MSI" }
    if ($clean -match "^(Synology|SYNOLOGY)") { return "Synology" }
    if ($clean -match "^(QNAP|Qnap)") { return "QNAP" }
    if ($clean -match "^(Aruba|ARUBA)") { return "Aruba" }
    if ($clean -match "^(Juniper|JUNIPER)") { return "Juniper" }
    if ($clean -match "^(Fortinet|FORTINET)") { return "Fortinet" }
    if ($clean -match "^(OnePlus|ONEPLUS)") { return "OnePlus" }
    if ($clean -match "^(OPPO|Oppo)") { return "Oppo" }
    if ($clean -match "^(vivo|Vivo|VIVO)") { return "Vivo" }
    if ($clean -match "^(Realme|REALME)") { return "Realme" }
    if ($clean -match "^(LG|LGElectronics)") { return "LG" }
    if ($clean -match "^(Motorola|MOTOROLA)") { return "Motorola" }
    if ($clean -match "^(VMware|VMWARE)") { return "VMware" }
    if ($clean -match "^(Nintendo|NINTENDO)") { return "Nintendo" }
    if ($clean -match "^(Honor|HONOR)") { return "Honor" }
    if ($clean -match "^(Tenda|TENDA)") { return "Tenda" }
    if ($clean -match "^(ZTE|Zte)") { return "ZTE" }
    
    # Strip trailing corporate noise
    $clean = $clean -replace '(?i),?\s*(Inc\.?|Incorporated|LLC|Corp\.?|Corporation|Ltd\.?|Limited|Co\.?|Company|GmbH|S\.A\.|SIA|Holdings?|Technologies?)$', ''
    return $clean.Trim()
}

$ouiMap = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)

$lines = $rawText -split "`r?`n"
foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) { continue }
    
    # Match standard 24-bit OUI: "00:00:0C \t Cisco \t Cisco Systems, Inc" or "00-00-0C \t Cisco"
    if ($line -match '^([0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2})\s+([^\t\r\n#]+)(?:\t(.*))?') {
        $prefix = $matches[1].Replace('-', ':').ToUpperInvariant()
        $shortName = $matches[2].Trim()
        $fullName = if ($matches.Count -ge 4 -and $matches[3]) { $matches[3].Trim() } else { "" }
        
        $vendor = Normalize-VendorName $shortName $fullName
        if ($vendor -and $vendor -ne "Unknown" -and -not $ouiMap.ContainsKey($prefix)) {
            $ouiMap[$prefix] = $vendor
        }
    }
}

Write-Host "Successfully parsed $($ouiMap.Count) distinct 24-bit IEEE OUIs!" -ForegroundColor Green

# 3. Categorize into top brands for fast in-memory dictionary
$targetBrands = @(
    "Apple", "Samsung", "Xiaomi", "Huawei", "Honor", "MikroTik", "TP-Link", "Intel",
    "Cisco", "Dell", "HP", "Lenovo", "ASUS", "Netgear", "D-Link", "Ubiquiti",
    "Google", "Amazon", "Microsoft", "Sony", "Nintendo", "VMware", "Realtek",
    "Broadcom", "GIGABYTE", "ASRock", "MSI", "Synology", "QNAP", "Aruba", "Juniper",
    "Fortinet", "OnePlus", "Oppo", "Vivo", "Realme", "LG", "Motorola", "Hikvision",
    "Dahua", "Sonos", "Roku", "Espressif (IoT)", "Raspberry Pi Foundation", "Tenda", "ZTE"
)

$categorized = [System.Collections.Generic.SortedDictionary[string, [System.Collections.Generic.List[string]]]]::new()
foreach ($brand in $targetBrands) {
    $categorized[$brand] = [System.Collections.Generic.List[string]]::new()
}
$otherList = [System.Collections.Generic.List[PSCustomObject]]::new()

foreach ($kv in $ouiMap.GetEnumerator()) {
    $matched = $false
    foreach ($brand in $targetBrands) {
        if ($kv.Value -eq $brand) {
            $categorized[$brand].Add($kv.Key)
            $matched = $true
            break
        }
    }
    if (-not $matched) {
        $otherList.Add([PSCustomObject]@{ Prefix = $kv.Key; Vendor = $kv.Value })
    }
}

# 4. Generate C# Code for Data/VendorLookup.cs
Write-Host "Generating modernized Data/VendorLookup.cs..." -ForegroundColor Yellow

$sb = [System.Text.StringBuilder]::new()
$null = $sb.AppendLine("using System;")
$null = $sb.AppendLine("using System.Collections.Generic;")
$null = $sb.AppendLine()
$null = $sb.AppendLine("namespace NodeRadarPro.Data;")
$null = $sb.AppendLine()
$null = $sb.AppendLine("/// <summary>")
$null = $sb.AppendLine("/// Maps MAC address prefixes (OUI) to hardware manufacturers.")
$null = $sb.AppendLine("/// Auto-generated from official IEEE / Wireshark registry via scripts/Update-OuiDatabase.ps1.")
$null = $sb.AppendLine("/// </summary>")
$null = $sb.AppendLine("public static class VendorLookup")
$null = $sb.AppendLine("{")
$null = $sb.AppendLine("    private static readonly Dictionary<string, string> _vendors = new(StringComparer.OrdinalIgnoreCase);")
$null = $sb.AppendLine()
$null = $sb.AppendLine("    static VendorLookup()")
$null = $sb.AppendLine("    {")

foreach ($brand in $targetBrands) {
    $prefixes = $categorized[$brand]
    if ($prefixes.Count -eq 0) { continue }
    
    $null = $sb.AppendLine("        // -- $brand ($($prefixes.Count) prefixes) --")
    for ($i = 0; $i -lt $prefixes.Count; $i += 3) {
        $chunk = $prefixes | Select-Object -Skip $i -First 3
        $lineCalls = ($chunk | ForEach-Object { "Add(`"$_`", `"$brand`");" }) -join " "
        $null = $sb.AppendLine("        $lineCalls")
    }
    $null = $sb.AppendLine()
}

$null = $sb.AppendLine("    }")
$null = $sb.AppendLine()
$null = $sb.AppendLine("    private static void Add(string key, string value) => _vendors[key] = value;")
$null = $sb.AppendLine()
$null = $sb.AppendLine("    public static string GetVendor(string macAddress)")
$null = $sb.AppendLine("    {")
$null = $sb.AppendLine("        if (string.IsNullOrEmpty(macAddress) || macAddress.Length < 8)")
$null = $sb.AppendLine("            return `"Unknown Vendor`";")
$null = $sb.AppendLine()
$null = $sb.AppendLine("        string prefix = macAddress[..8].Replace(`"-`", `":`").ToUpperInvariant();")
$null = $sb.AppendLine()
$null = $sb.AppendLine("        if (_vendors.TryGetValue(prefix, out string? vendor))")
$null = $sb.AppendLine("            return vendor;")
$null = $sb.AppendLine()
$null = $sb.AppendLine("        // Check for MAC Randomization (Locally Administered Bit is set)")
$null = $sb.AppendLine("        if (macAddress.Length >= 2)")
$null = $sb.AppendLine("        {")
$null = $sb.AppendLine("            char c = macAddress[1];")
$null = $sb.AppendLine("            if (c == '2' || c == '6' || c == 'A' || c == 'E' || c == 'a' || c == 'e')")
$null = $sb.AppendLine("                return `"Randomized MAC (Mobile/Privacy)`";")
$null = $sb.AppendLine("        }")
$null = $sb.AppendLine()
$null = $sb.AppendLine("        return `"Unknown Vendor`";")
$null = $sb.AppendLine("    }")
$null = $sb.AppendLine()
$null = $sb.AppendLine("    /// <summary>")
$null = $sb.AppendLine("    /// Deprecated backwards-compatible shim for legacy tests and callers.")
$null = $sb.AppendLine("    /// </summary>")
$null = $sb.AppendLine("#pragma warning disable S1133")
$null = $sb.AppendLine("    [Obsolete(`"Guessing is now completely offloaded to DeviceClassifierEngine.`")]")
$null = $sb.AppendLine("    public static string GuessDeviceType(string vendor, string hostname)")
$null = $sb.AppendLine("    {")
$null = $sb.AppendLine("        return `"Generic Network Device`";")
$null = $sb.AppendLine("    }")
$null = $sb.AppendLine("#pragma warning restore S1133")
$null = $sb.AppendLine("}")
$null = $sb.AppendLine()

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($vendorLookupFile, $sb.ToString(), $utf8NoBom)
Write-Host "Updated Data/VendorLookup.cs with $(($categorized.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum) curated vendor prefixes!" -ForegroundColor Green

# 5. Optional JSON Export
if ($ExportJson) {
    Write-Host "Exporting full OUI database to $jsonExportFile..." -ForegroundColor Yellow
    $exportList = @()
    foreach ($kv in $ouiMap.GetEnumerator()) {
        $exportList += [PSCustomObject]@{ Prefix = $kv.Key; Vendor = $kv.Value }
    }
    $exportList | ConvertTo-Json -Compress | Out-File -FilePath $jsonExportFile -Encoding utf8
    Write-Host "Exported $($exportList.Count) entries to JSON." -ForegroundColor Green
}

# 6. Verification
if ($Verify) {
    Write-Host "`nFormatting and verifying solution..." -ForegroundColor Yellow
    Push-Location $root
    try {
        & dotnet format (Join-Path $root "NodeRadarPro.slnx") --no-restore
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Test-MasterGate.ps1")
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Master Gate validation failed!" -ForegroundColor Red
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "`n== OUI Database Update Pipeline Complete ==" -ForegroundColor Green
