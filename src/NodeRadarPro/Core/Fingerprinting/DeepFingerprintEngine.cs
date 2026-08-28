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

    public DeepFingerprintEngine() : this(new IFingerprintProbe[]
    {
        new MacOuiProbe(),
        new MdnsProbe(),
        new SsdpProbe(),
        new SnmpProbe(),
        new NbnsProbe(),
        new BannerGrabProbe()
    })
    {
    }

    internal DeepFingerprintEngine(IEnumerable<IFingerprintProbe> probes)
    {
        _probes = probes?.ToList() ?? new List<IFingerprintProbe>();
    }

    /// <summary>
    /// Starts background broadcast probes (mDNS, SSDP) to populate the caches.
    /// Should be called at the beginning of a subnet sweep.
    /// </summary>
    public async Task StartDiscoverySweepAsync(CancellationToken externalToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        cts.CancelAfter(4000); // 4 seconds sweep

        var mdnsTask = Task.Run(() => MdnsProbe.StartSweepAsync(cts.Token), cts.Token);
        var ssdpTask = Task.Run(() => SsdpProbe.StartSweepAsync(cts.Token), cts.Token);

        try
        {
            await Task.WhenAll(mdnsTask, ssdpTask);
        }
        catch (OperationCanceledException) { }
        catch { }
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
            catch (OperationCanceledException)
            {
                Logger.Log(LogLevel.Info, "Fingerprinter", $"Probe {probe.Name} canceled.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "Fingerprinter", $"Probe {probe.Name} failed: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);

        return DeviceClassifierEngine.Classify(node, probeResults);
    }
}
