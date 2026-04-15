using System;
using System.Collections.Generic;
using System.IO;

namespace NodeRadarPro.Data;

/// <summary>
/// Embeds a small offline database to map MAC addresses to hardware vendors.
/// </summary>
public static class VendorLookup
{
    // A simplified dictionary to map MAC prefixes to manufacturers.
    // In production, we'd deserialize a full JSON file like mac-vendors.json here.
    private static readonly Dictionary<string, string> _vendors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "00:00:0C", "Cisco Systems, Inc" },
        { "00:03:93", "Apple, Inc." },
        { "00:05:69", "VMware, Inc." },
        { "00:0A:95", "Apple, Inc." },
        { "00:0C:29", "VMware, Inc." },
        { "00:10:18", "Broadcom" },
        { "00:11:11", "Intel Corporation" },
        { "00:14:22", "Dell Inc." },
        { "00:1A:11", "Google, Inc." },
        { "00:1B:63", "Apple, Inc." },
        { "00:1E:8C", "AsusTek Computer Inc." },
        { "00:23:14", "Intel Corporate" },
        { "00:24:E4", "Withings" },
        { "00:25:9C", "Cisco Systems, Inc" },
        { "00:50:56", "VMware, Inc." },
        { "04:69:F8", "Apple, Inc." },
        { "28:CF:E9", "Apple, Inc." },
        { "3C:D9:2B", "Hewlett Packard" },
        { "44:03:2C", "Intel Corporate" },
        { "4C:EB:42", "Nokia Corporation" },
        { "5C:A3:9D", "Sony Mobile Communications" },
        { "64:16:7F", "Polycom" },
        { "74:D4:35", "GIGABYTE TECHNOLOGY CO.,LTD." }, // Your Mobo!
        { "90:B1:1C", "Dell Inc." },
        { "AC:87:A3", "Microsoft Corporation" },
        { "B4:B6:76", "Intel Corporate" },
        { "BC:5F:F4", "ASRock Inc." },
        { "C0:3F:0E", "Netgear Inc." },
        { "CC:52:AF", "Microsoft Corporation" },
        { "D8:50:E6", "ASUSTek COMPUTER INC." },
        { "E0:D4:64", "Sony Interactive Entertainment AB" },
        { "F8:1E:DF", "Samsung Electronics Co.,Ltd" }
    };

    public static string GetVendor(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress) || macAddress.Length < 8)
            return "Unknown Vendor";

        string prefix = macAddress.Substring(0, 8).Replace("-", ":").ToUpper();
        
        if (_vendors.TryGetValue(prefix, out string? vendor))
        {
            return vendor;
        }

        return "Unknown Vendor";
    }
}