using System;
using System.Collections.Generic;

namespace NodeRadarPro.Data;

/// <summary>
/// Maps MAC address prefixes (OUI) to hardware manufacturers.
/// Covers ~200 of the most common vendors seen on real networks.
/// </summary>
public static class VendorLookup
{
    private static readonly Dictionary<string, string> _vendors = new(StringComparer.OrdinalIgnoreCase);

    static VendorLookup()
    {
        // ── Apple ──
        Add("00:03:93", "Apple"); Add("00:0A:95", "Apple"); Add("00:1B:63", "Apple");
        Add("04:69:F8", "Apple"); Add("28:CF:E9", "Apple"); Add("3C:15:C2", "Apple");
        Add("40:33:1A", "Apple"); Add("60:FA:CD", "Apple"); Add("7C:D1:C3", "Apple");
        Add("88:66:A5", "Apple"); Add("90:8D:6C", "Apple"); Add("A8:20:66", "Apple");
        Add("A8:88:08", "Apple"); Add("AC:BC:32", "Apple"); Add("B0:34:95", "Apple");
        Add("B8:53:AC", "Apple"); Add("B8:E8:56", "Apple"); Add("C0:A5:3E", "Apple");
        Add("C8:69:CD", "Apple"); Add("CC:08:E0", "Apple"); Add("D0:81:7A", "Apple");
        Add("D4:61:9D", "Apple"); Add("DC:A9:04", "Apple"); Add("E0:C7:67", "Apple");
        Add("F0:D1:A9", "Apple"); Add("F4:5C:89", "Apple"); Add("F8:1E:DF", "Apple");

        // ── Samsung ──
        Add("00:16:6B", "Samsung"); Add("00:16:DB", "Samsung"); Add("00:24:90", "Samsung");
        Add("08:FC:88", "Samsung"); Add("10:D5:42", "Samsung"); Add("14:49:E0", "Samsung");
        Add("1C:62:B8", "Samsung"); Add("24:18:1D", "Samsung"); Add("28:98:7B", "Samsung");
        Add("2C:AE:2B", "Samsung"); Add("30:CD:A7", "Samsung"); Add("34:23:BA", "Samsung");
        Add("38:01:46", "Samsung"); Add("3C:A1:0D", "Samsung"); Add("40:4E:36", "Samsung");
        Add("4C:BC:98", "Samsung"); Add("50:01:BB", "Samsung"); Add("54:92:BE", "Samsung");
        Add("58:C3:8B", "Samsung"); Add("6C:2F:2C", "Samsung"); Add("78:D6:F0", "Samsung");
        Add("78:BD:BC", "Samsung"); Add("84:38:38", "Samsung"); Add("8C:77:12", "Samsung");
        Add("9C:3A:AF", "Samsung"); Add("A0:CC:2B", "Samsung"); Add("A8:06:00", "Samsung");
        Add("AC:5F:3E", "Samsung"); Add("B4:07:F9", "Samsung"); Add("BC:14:EF", "Samsung");
        Add("C4:73:1E", "Samsung"); Add("CC:07:AB", "Samsung"); Add("D0:22:BE", "Samsung");
        Add("D0:66:7B", "Samsung"); Add("D8:90:E8", "Samsung"); Add("E4:7C:F9", "Samsung");
        Add("EC:1F:72", "Samsung"); Add("F0:25:B7", "Samsung"); Add("F4:7B:5E", "Samsung");

        // ── Xiaomi ──
        Add("04:CF:8C", "Xiaomi"); Add("0C:1D:AF", "Xiaomi"); Add("10:2A:B3", "Xiaomi");
        Add("14:F6:5A", "Xiaomi"); Add("18:59:36", "Xiaomi"); Add("20:47:DA", "Xiaomi");
        Add("28:6C:07", "Xiaomi"); Add("2C:28:B7", "Xiaomi"); Add("34:CE:00", "Xiaomi");
        Add("38:A4:ED", "Xiaomi"); Add("3C:BD:3E", "Xiaomi"); Add("44:23:7C", "Xiaomi");
        Add("50:64:2B", "Xiaomi"); Add("58:44:98", "Xiaomi"); Add("64:B4:73", "Xiaomi");
        Add("7C:1D:D9", "Xiaomi"); Add("8C:5A:C1", "Xiaomi"); Add("98:FA:E3", "Xiaomi");

        // ── Huawei / Honor ──
        Add("00:18:82", "Huawei"); Add("00:1E:10", "Huawei"); Add("00:46:4B", "Huawei");
        Add("04:F9:38", "Huawei"); Add("0C:37:DC", "Huawei"); Add("10:47:80", "Huawei");
        Add("14:B9:68", "Huawei"); Add("20:A6:80", "Huawei"); Add("24:09:95", "Huawei");
        Add("28:31:52", "Huawei"); Add("30:D1:7E", "Huawei"); Add("34:CD:B0", "Huawei");
        Add("38:4C:4F", "Huawei"); Add("40:4D:8E", "Huawei"); Add("48:46:FB", "Huawei");
        Add("4C:B1:6C", "Huawei"); Add("54:A5:1B", "Huawei"); Add("58:60:5F", "Huawei");
        Add("5C:7D:5E", "Huawei"); Add("60:DE:44", "Huawei"); Add("70:7B:E8", "Huawei");
        Add("78:F5:FD", "Huawei"); Add("80:41:26", "Huawei"); Add("88:28:B3", "Huawei");
        Add("9C:28:EF", "Huawei"); Add("A4:99:47", "Huawei"); Add("AC:CF:85", "Huawei");
        Add("B0:E1:A5", "Huawei"); Add("C0:70:09", "Huawei"); Add("CC:A2:23", "Huawei");
        Add("D0:7A:B5", "Huawei"); Add("E4:68:A3", "Huawei"); Add("F4:63:49", "Huawei");

        // ── MikroTik ──
        Add("00:0C:42", "MikroTik"); Add("2C:C8:1B", "MikroTik"); Add("4C:5E:0C", "MikroTik");
        Add("6C:3B:6B", "MikroTik"); Add("74:4D:28", "MikroTik"); Add("B8:69:F4", "MikroTik");
        Add("C4:AD:34", "MikroTik"); Add("CC:2D:E0", "MikroTik"); Add("D4:01:C3", "MikroTik");
        Add("DC:2C:6E", "MikroTik"); Add("E4:8D:8C", "MikroTik"); Add("48:A9:8A", "MikroTik");
        Add("08:55:31", "MikroTik"); Add("18:FD:74", "MikroTik");

        // ── TP-Link ──
        Add("00:31:92", "TP-Link"); Add("10:FE:ED", "TP-Link"); Add("14:CC:20", "TP-Link");
        Add("18:D6:C7", "TP-Link"); Add("1C:3B:F3", "TP-Link"); Add("30:B5:C2", "TP-Link");
        Add("50:C7:BF", "TP-Link"); Add("54:C8:0F", "TP-Link"); Add("5C:A6:E6", "TP-Link");
        Add("60:32:B1", "TP-Link"); Add("64:70:02", "TP-Link"); Add("6C:5A:B0", "TP-Link");
        Add("78:8C:B5", "TP-Link"); Add("90:F6:52", "TP-Link"); Add("98:DA:C4", "TP-Link");
        Add("A0:F3:C1", "TP-Link"); Add("AC:84:C6", "TP-Link"); Add("B0:4E:26", "TP-Link");
        Add("C0:06:C3", "TP-Link"); Add("C0:E3:FB", "TP-Link"); Add("D8:07:B6", "TP-Link");
        Add("E8:DE:27", "TP-Link"); Add("EC:08:6B", "TP-Link"); Add("F4:F2:6D", "TP-Link");

        // ── Intel ──
        Add("00:11:11", "Intel"); Add("00:23:14", "Intel"); Add("3C:97:0E", "Intel");
        Add("44:03:2C", "Intel"); Add("5C:87:9C", "Intel"); Add("68:05:CA", "Intel");
        Add("80:86:F2", "Intel"); Add("8C:8D:28", "Intel"); Add("A4:BF:01", "Intel");
        Add("B4:B6:76", "Intel"); Add("B4:96:91", "Intel"); Add("C8:D3:FF", "Intel");
        Add("D0:94:66", "Intel"); Add("F8:63:3F", "Intel"); Add("48:A4:72", "Intel");

        // ── Cisco ──
        Add("00:00:0C", "Cisco"); Add("00:25:9C", "Cisco"); Add("00:1E:7A", "Cisco");
        Add("00:22:BD", "Cisco"); Add("1C:E6:C7", "Cisco"); Add("34:62:88", "Cisco");
        Add("58:97:1E", "Cisco"); Add("6C:41:6A", "Cisco"); Add("A4:56:30", "Cisco");
        Add("BC:67:1C", "Cisco"); Add("CC:46:D6", "Cisco"); Add("F0:29:29", "Cisco");

        // ── Dell ──
        Add("00:14:22", "Dell"); Add("14:FE:B5", "Dell"); Add("18:A9:05", "Dell");
        Add("24:B6:FD", "Dell"); Add("34:17:EB", "Dell"); Add("54:9F:35", "Dell");
        Add("74:86:7A", "Dell"); Add("90:B1:1C", "Dell"); Add("B0:83:FE", "Dell");
        Add("D4:81:D7", "Dell"); Add("F0:1F:AF", "Dell"); Add("F8:BC:12", "Dell");

        // ── HP ──
        Add("00:1A:4B", "HP"); Add("00:23:7D", "HP"); Add("10:1F:74", "HP");
        Add("14:58:D0", "HP"); Add("28:92:4A", "HP"); Add("30:8D:99", "HP");
        Add("3C:D9:2B", "HP"); Add("48:0F:CF", "HP"); Add("5C:B9:01", "HP");
        Add("6C:C2:17", "HP"); Add("8C:DC:D4", "HP"); Add("94:57:A5", "HP");
        Add("A0:D3:C1", "HP"); Add("B0:5A:DA", "HP"); Add("E4:11:5B", "HP");

        // ── Lenovo ──
        Add("00:1E:4F", "Lenovo"); Add("28:D2:44", "Lenovo");
        Add("40:B0:34", "Lenovo"); Add("54:EE:75", "Lenovo"); Add("68:F7:28", "Lenovo");
        Add("8C:16:45", "Lenovo"); Add("98:E7:F4", "Lenovo"); Add("C8:5B:76", "Lenovo");

        // ── ASUS ──
        Add("00:1E:8C", "ASUS"); Add("04:D4:C4", "ASUS"); Add("10:C3:7B", "ASUS");
        Add("2C:56:DC", "ASUS"); Add("30:5A:3A", "ASUS"); Add("40:B0:76", "ASUS");
        Add("50:46:5D", "ASUS"); Add("70:8B:CD", "ASUS"); Add("88:D7:F6", "ASUS");
        Add("D8:50:E6", "ASUS"); Add("F0:79:59", "ASUS"); Add("AC:9E:17", "ASUS");

        // ── Netgear ──
        Add("00:14:6C", "Netgear"); Add("00:24:B2", "Netgear"); Add("20:0C:C8", "Netgear");
        Add("2C:B0:5D", "Netgear"); Add("44:94:FC", "Netgear"); Add("6C:B0:CE", "Netgear");
        Add("84:1B:5E", "Netgear"); Add("9C:3D:CF", "Netgear"); Add("A4:2B:8C", "Netgear");
        Add("B0:7F:B9", "Netgear"); Add("C0:3F:0E", "Netgear"); Add("E0:46:9A", "Netgear");

        // ── D-Link ──
        Add("00:1C:F0", "D-Link"); Add("14:D6:4D", "D-Link"); Add("1C:7E:E5", "D-Link");
        Add("28:10:7B", "D-Link"); Add("34:08:04", "D-Link"); Add("60:63:4C", "D-Link");
        Add("78:54:2E", "D-Link"); Add("90:94:E4", "D-Link"); Add("B8:A3:86", "D-Link");
        Add("C8:D3:A3", "D-Link"); Add("F0:7D:68", "D-Link"); Add("FC:75:16", "D-Link");

        // ── Ubiquiti ──
        Add("00:15:6D", "Ubiquiti"); Add("04:18:D6", "Ubiquiti"); Add("18:E8:29", "Ubiquiti");
        Add("24:5A:4C", "Ubiquiti"); Add("44:D9:E7", "Ubiquiti"); Add("68:72:51", "Ubiquiti");
        Add("78:8A:20", "Ubiquiti"); Add("80:2A:A8", "Ubiquiti"); Add("B4:FB:E4", "Ubiquiti");
        Add("DC:9F:DB", "Ubiquiti"); Add("F0:9F:C2", "Ubiquiti"); Add("FC:EC:DA", "Ubiquiti");

        // ── Google / Nest ──
        Add("00:1A:11", "Google"); Add("08:9E:08", "Google"); Add("3C:F7:A4", "Google");
        Add("3C:5A:B4", "Google"); Add("54:60:09", "Google"); Add("94:EB:2C", "Google");
        Add("A4:77:33", "Google"); Add("F4:F5:D8", "Google"); Add("F4:F5:E8", "Google");

        // ── Amazon / Ring / Echo ──
        Add("10:CE:A9", "Amazon"); Add("18:74:2E", "Amazon"); Add("34:D2:70", "Amazon");
        Add("40:B4:CD", "Amazon"); Add("44:65:0D", "Amazon"); Add("50:DC:E7", "Amazon");
        Add("68:54:FD", "Amazon"); Add("74:C2:46", "Amazon"); Add("84:D6:D0", "Amazon");
        Add("A0:02:DC", "Amazon"); Add("AC:63:BE", "Amazon"); Add("FC:65:DE", "Amazon");

        // ── Microsoft / Xbox ──
        Add("AC:87:A3", "Microsoft"); Add("CC:52:AF", "Microsoft"); Add("28:18:78", "Microsoft");
        Add("60:45:BD", "Microsoft"); Add("7C:1E:52", "Microsoft"); Add("DC:B4:C4", "Microsoft");

        // ── Sony PlayStation ──
        Add("00:D9:D1", "Sony"); Add("28:3F:69", "Sony"); Add("5C:A3:9D", "Sony");
        Add("70:9E:29", "Sony"); Add("A8:E3:EE", "Sony"); Add("E0:D4:64", "Sony");

        // ── Nintendo ──
        Add("00:17:AB", "Nintendo"); Add("00:22:D7", "Nintendo"); Add("00:24:F3", "Nintendo");
        Add("34:AF:2C", "Nintendo"); Add("40:F4:07", "Nintendo"); Add("58:2F:40", "Nintendo");
        Add("7C:BB:8A", "Nintendo"); Add("98:B6:E9", "Nintendo");

        // ── VMware ──
        Add("00:05:69", "VMware"); Add("00:0C:29", "VMware"); Add("00:50:56", "VMware");

        // ── Realtek / Broadcom ──
        Add("00:10:18", "Broadcom"); Add("00:E0:4C", "Realtek"); Add("48:5B:39", "Realtek");
        Add("52:54:00", "Realtek"); Add("80:1F:12", "Realtek"); Add("E8:4E:06", "Realtek");

        // ── GIGABYTE / ASRock / MSI ──
        Add("74:D4:35", "GIGABYTE"); Add("BC:5F:F4", "ASRock"); Add("00:D8:61", "MSI");
        Add("4C:E1:73", "MSI"); Add("04:7C:16", "MSI");

        // ── Synology / QNAP ──
        Add("00:11:32", "Synology"); Add("00:08:9B", "QNAP");

        // ── Aruba / Juniper / Fortinet ──
        Add("00:0B:86", "Aruba"); Add("00:1B:C5", "Aruba"); Add("24:DE:C6", "Aruba");
        Add("38:94:ED", "Aruba"); Add("6C:F3:7F", "Aruba");
        Add("00:23:9C", "Juniper"); Add("3C:61:04", "Juniper"); Add("4C:96:14", "Juniper");
        Add("00:09:0F", "Fortinet"); Add("70:4C:A5", "Fortinet");

        // ── Xiaomi / Huawei / Samsung (Modern Additions) ──
        Add("00:9A:CD", "Huawei"); Add("04:33:89", "Huawei"); Add("0C:96:E6", "Huawei");
        Add("10:1B:54", "Huawei"); Add("20:08:ED", "Huawei"); Add("24:DF:6A", "Huawei");
        Add("48:43:5A", "Huawei"); Add("60:DE:F3", "Huawei"); Add("80:B6:86", "Huawei");

        Add("00:EC:0A", "Xiaomi"); Add("0C:1D:AF", "Xiaomi"); Add("14:F6:5A", "Xiaomi");
        Add("28:E3:1F", "Xiaomi"); Add("3C:BD:3E", "Xiaomi"); Add("50:8A:06", "Xiaomi");
        Add("64:09:80", "Xiaomi"); Add("74:51:BA", "Xiaomi"); Add("8C:45:00", "Xiaomi");
        Add("98:FA:E3", "Xiaomi"); Add("AC:C1:EE", "Xiaomi"); Add("C4:0B:CB", "Xiaomi");
        Add("D4:A1:48", "Xiaomi"); Add("F4:J6:B3", "Xiaomi"); Add("FC:64:BA", "Xiaomi");

        Add("00:12:36", "Samsung"); Add("00:15:99", "Samsung"); Add("00:1E:E3", "Samsung");
        Add("00:26:37", "Samsung"); Add("08:D4:6A", "Samsung"); Add("10:19:94", "Samsung");
        Add("14:89:FD", "Samsung"); Add("20:1F:3B", "Samsung"); Add("28:CC:01", "Samsung");
        Add("38:AA:3C", "Samsung"); Add("48:4B:AA", "Samsung"); Add("50:85:69", "Samsung");
        Add("5C:A8:6A", "Samsung"); Add("60:6B:FF", "Samsung"); Add("64:B3:10", "Samsung");
        Add("70:F1:A1", "Samsung"); Add("78:59:5E", "Samsung"); Add("84:0B:2D", "Samsung");

        // ── OnePlus / Oppo / Vivo / Realme / Tecno ──
        Add("94:65:2D", "OnePlus"); Add("C0:EE:FB", "OnePlus"); Add("50:01:D9", "OnePlus");
        Add("00:1E:AC", "Oppo"); Add("A4:3B:FA", "Oppo"); Add("84:B8:B8", "Oppo");
        Add("24:11:05", "Vivo"); Add("60:0F:77", "Vivo"); Add("9C:52:F8", "Vivo");
        Add("3C:05:18", "Realme"); Add("D8:45:1E", "Realme");
        Add("48:A1:95", "Tecno"); Add("70:86:CE", "Tecno");
        Add("D0:53:49", "Liteon/Laptop WiFi");

        // ── LG ──
        Add("00:1E:75", "LG"); Add("00:22:A9", "LG"); Add("10:68:3F", "LG");
        Add("34:4D:F7", "LG"); Add("58:A2:B5", "LG"); Add("88:07:4B", "LG");
        Add("A8:23:FE", "LG"); Add("CC:FA:00", "LG"); Add("F8:0C:F3", "LG");

        // ── Motorola / Google Pixel ──
        Add("00:04:56", "Motorola"); Add("00:0C:E5", "Motorola"); Add("14:A5:1A", "Motorola");
        Add("34:BB:26", "Motorola"); Add("7C:46:85", "Motorola"); Add("D4:7A:E2", "Motorola");

        // ── Hikvision / Dahua (Cameras) ──
        Add("28:57:BE", "Hikvision"); Add("44:19:B6", "Hikvision"); Add("54:C4:15", "Hikvision");
        Add("C0:56:27", "Hikvision"); Add("F4:15:63", "Hikvision");
        Add("3C:EF:8C", "Dahua"); Add("A0:BD:1D", "Dahua");

        // ── Sonos ──
        Add("00:0E:58", "Sonos"); Add("34:7E:5C", "Sonos"); Add("5C:AA:FD", "Sonos");
        Add("78:28:CA", "Sonos"); Add("94:9F:3E", "Sonos"); Add("B8:E9:37", "Sonos");

        // ── Roku / Smart TV ──
        Add("00:0D:4B", "Roku"); Add("20:EF:BD", "Roku"); Add("B0:A7:37", "Roku");
        Add("CC:6D:A0", "Roku"); Add("D0:4D:C6", "Roku"); Add("D8:31:34", "Roku");

        // ── Obscure & Industrial (The "Break the Internet" Tier) ──
        Add("00:01:02", "3COM"); Add("00:01:42", "Cisco-Linksys"); Add("00:02:72", "CC&C Technologies");
        Add("00:04:F2", "Polycom"); Add("00:05:04", "Nortel"); Add("00:05:B1", "ASRock");
        Add("00:07:32", "Aureal"); Add("00:08:5D", "Auvidea"); Add("00:0B:AD", "Fiberhome");
        Add("00:0C:E7", "Marvell"); Add("00:0E:08", "Microsoft (Surface)"); Add("00:0E:8E", "AzureWave");
        Add("00:11:6B", "Iomega"); Add("00:13:72", "Dell (Legacy)"); Add("00:15:AF", "AzureWave (IoT)");
        Add("00:16:D3", "TCL"); Add("00:17:88", "Philips Hue"); Add("00:17:C5", "Sagemcom");
        Add("00:1A:2B", "Casio"); Add("00:1D:BA", "Sony Mobile"); Add("00:1E:E5", "D-Link");
        Add("00:21:2F", "Pinnacle"); Add("00:23:69", "Juniper"); Add("00:25:22", "ASRock");
        Add("00:26:BB", "Apple (Legacy)"); Add("00:26:ED", "Foscam"); Add("00:30:1B", "Shuttle");
        Add("00:40:5A", "Opto 22 (Industrial)"); Add("00:50:C2", "Sun Microsystems"); Add("00:60:E0", "SMC Networks");
        Add("00:80:C8", "D-Link (Industrial)"); Add("00:90:7F", "A-Trend"); Add("00:90:A9", "Western Digital");
        Add("00:A0:C9", "Intel"); Add("00:D0:2D", "WNC (IoT)"); Add("00:E0:4B", "Realtek");
        Add("04:18:B6", "Ubiquiti"); Add("04:52:F3", "Sony"); Add("04:A1:51", "NETGEAR");
        Add("04:D4:C4", "ASUS"); Add("04:F0:21", "Compal (Laptops)"); Add("08:00:20", "Sun");
        Add("08:60:6E", "ASUS"); Add("08:9E:01", "Technicolor"); Add("0C:47:3D", "Super Micro");
        Add("10:08:B1", "WNC"); Add("10:60:4B", "ASUS"); Add("14:2D:27", "AzureWave");
        Add("18:31:BF", "ASUS"); Add("1C:1A:DF", "Super Micro"); Add("20:4C:9A", "Dell");
        Add("24:BE:05", "Hewlett Packard"); Add("28:10:7B", "D-Link"); Add("2C:30:33", "NETGEAR");
        Add("30:85:A9", "ASUS"); Add("34:64:A9", "Hewlett Packard"); Add("38:22:D6", "Ubiquiti");
        Add("3C:08:F6", "Arris"); Add("40:16:7E", "ASROCK"); Add("44:D9:E7", "Ubiquiti");
        Add("48:D2:4F", "Apple"); Add("4C:72:B9", "Schneider Electric"); Add("50:65:F3", "Hewlett Packard");
        Add("54:04:A6", "ASUS"); Add("58:D9:C3", "Cisco"); Add("5C:A4:8A", "Dell");
        Add("60:45:BD", "Microsoft"); Add("64:00:6A", "Dell"); Add("68:3E:34", "Cisco");
        Add("6C:F3:7F", "Aruba"); Add("70:5A:0F", "Hewlett Packard"); Add("74:D4:35", "GIGABYTE");
        Add("78:24:AF", "ASUS"); Add("7C:05:07", "AzureWave"); Add("80:CE:62", "Samsung");
        Add("84:A6:C8", "Intel"); Add("88:12:4E", "ASUS"); Add("8C:85:90", "Apple");
        Add("90:B1:1C", "Dell"); Add("94:65:9C", "Sagemcom"); Add("98:01:A7", "AzureWave");
        Add("9C:7B:D2", "Sagemcom"); Add("A0:21:B7", "Intel"); Add("A4:77:33", "Google");
        Add("A8:5B:78", "Hon Hai (Foxconn)"); Add("AC:17:C8", "MikroTik"); Add("B0:BE:76", "TP-Link");
        Add("B4:75:0E", "AzureWave"); Add("B8:27:EB", "Raspberry Pi Foundation"); Add("BC:54:36", "Samsung");
        Add("C0:3F:0E", "NETGEAR"); Add("C4:AD:34", "MikroTik"); Add("C8:3A:35", "Tenda");
        Add("CC:2D:21", "Sagemcom"); Add("D0:03:4B", "Apple"); Add("D4:6E:0E", "TP-Link");
        Add("D8:0D:17", "TP-Link"); Add("DC:53:7C", "Sagemcom"); Add("E0:3F:49", "ASUS");
        Add("E4:8D:8C", "MikroTik"); Add("E8:94:F6", "TP-Link"); Add("EC:08:6B", "TP-Link");
        Add("F0:79:59", "ASUS"); Add("F4:8E:38", "Sagemcom"); Add("F8:E9:03", "Intel");
        Add("FC:FB:FB", "Cisco");
    }

    private static void Add(string key, string value) => _vendors[key] = value;

    public static string GetVendor(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress) || macAddress.Length < 8)
            return "Unknown Vendor";

        string prefix = macAddress[..8].Replace("-", ":").ToUpper();

        if (_vendors.TryGetValue(prefix, out string? vendor))
            return vendor;

        // Check for MAC Randomization (Locally Administered Bit is set)
        if (macAddress.Length >= 2)
        {
            char c = macAddress[1];
            if (c == '2' || c == '6' || c == 'A' || c == 'E' || c == 'a' || c == 'e')
                return "Randomized MAC (Mobile/Privacy)";
        }

        return "Unknown Vendor";
    }

    /// <summary>
    /// Guessing is now completely offloaded to the far superior DeviceClassifier.cs engine.
    /// This method is deprecated but kept temporarily so the UI doesn't break if it depends on it.
    /// </summary>
#pragma warning disable S1133
    [Obsolete("Guessing is now completely offloaded to the far superior DeviceClassifier.cs engine. This method is deprecated but kept temporarily so the UI doesn't break if it depends on it.")]
    public static string GuessDeviceType(string vendor, string hostname)
    {
        // For backwards compatibility before we rip it out entirely.
        // The real logic runs in Core.DeviceClassifier.ResolveDetails.
        return "Generic Network Device";
    }
#pragma warning restore S1133
}
