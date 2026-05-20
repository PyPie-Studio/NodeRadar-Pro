using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Core.Fingerprinting;

public class DeepFingerprintEngine
{
    private static readonly Lazy<DeepFingerprintEngine> _instance = new(() => new DeepFingerprintEngine());
    public static DeepFingerprintEngine Instance => _instance.Value;

    private readonly List<IFingerprintProbe> _probes;

    private DeepFingerprintEngine()
    {
        _probes = new List<IFingerprintProbe>
        {
            new MacOuiProbe(),
            new MdnsProbe(),
            new SsdpProbe(),
            new SnmpProbe(),
            new BannerGrabProbe()
        };
    }

    /// <summary>
    /// Starts background broadcast probes (mDNS, SSDP) to populate the caches.
    /// Should be called at the beginning of a subnet sweep.
    /// </summary>
    public async Task StartDiscoverySweepAsync()
    {
        using var cts = new CancellationTokenSource(4000); // 4 seconds sweep
        var mdnsTask = Task.Run(() => MdnsProbe.StartSweepAsync(cts.Token));
        var ssdpTask = Task.Run(() => SsdpProbe.StartSweepAsync(cts.Token));

        await Task.WhenAll(mdnsTask, ssdpTask);
    }

    /// <summary>
    /// Runs all fingerprinting probes against a single node, aggregates results, and classifies the device.
    /// </summary>
    public async Task<FingerprintResult> FingerprintNodeAsync(NetworkNode node, CancellationToken ct)
    {
        var probeResults = new List<ProbeResult>();

        // Execute probes ordered by priority concurrently
        var tasks = _probes.OrderBy(p => p.Priority).Select(async probe =>
        {
            try
            {
                // Each probe gets a 3-second absolute timeout to prevent hanging the whole process
                using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                probeCts.CancelAfter(3000);
                
                var result = await probe.ProbeAsync(node, probeCts.Token);
                lock (probeResults)
                {
                    probeResults.Add(result);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "Fingerprinter", $"Probe {probe.Name} failed: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);

        return DeviceClassifierEngine.Classify(node, probeResults);
    }
}
