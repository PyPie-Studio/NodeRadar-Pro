using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Fingerprinting;

public interface IFingerprintProbe
{
    string Name { get; }
    int Priority { get; } // Lower number = runs earlier/higher priority
    Task<ProbeResult> ProbeAsync(NetworkNode node, CancellationToken ct);
}
