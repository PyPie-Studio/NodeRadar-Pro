using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Data;

namespace NodeRadarPro.Core.Fingerprinting.Probes;

public class MacOuiProbe : IFingerprintProbe
{
    public string Name => "MAC OUI Lookup";
    public int Priority => 10;

    public Task<ProbeResult> ProbeAsync(NetworkNode node, CancellationToken ct)
    {
        var result = new ProbeResult { Source = Name };

        if (string.IsNullOrEmpty(node.MacAddress) || node.MacAddress.Length < 8 || node.MacAddress == "Unknown")
        {
            return Task.FromResult(result);
        }
        // 1. Check for MAC Randomization (Locally Administered Bit is set)
        if (node.MacAddress.Length >= 2)
        {
            char c = node.MacAddress[1];
            if (c == '2' || c == '6' || c == 'A' || c == 'E' || c == 'a' || c == 'e')
            {
                result.RawData["Vendor"] = "Randomized MAC (Mobile/Privacy)";
                return Task.FromResult(result);
            }
        }

        // 2. Query VendorLookup
        string vendor = VendorLookup.GetVendor(node.MacAddress);
        if (vendor != "Unknown Vendor")
        {
            result.RawData["Vendor"] = vendor;
        }

        return Task.FromResult(result);
    }
}
