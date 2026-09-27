using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NodeRadarPro.Tests.UI;

public class ScannerPageSynchronizationTests
{
    [Fact]
    public async Task Benchmark_PollingWait_Vs_AsyncSignal()
    {
        // Simulate polling loop wait
        int inFlightCountPolling = 1;
        var stopwatchPolling = Stopwatch.StartNew();

        var pollingTask = Task.Run(async () =>
        {
            while (Volatile.Read(ref inFlightCountPolling) > 0)
            {
                await Task.Delay(50);
            }
        });

        // Simulate work finishing after 5ms
        await Task.Delay(5);
        Interlocked.Decrement(ref inFlightCountPolling);
        await pollingTask;
        stopwatchPolling.Stop();

        // Simulate Async Signal wait (TaskCompletionSource)
        int inFlightCountSignal = 1;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopwatchSignal = Stopwatch.StartNew();

        var signalTask = Task.Run(async () =>
        {
            if (Volatile.Read(ref inFlightCountSignal) > 0)
            {
                await tcs.Task;
            }
        });

        // Simulate work finishing after 5ms
        await Task.Delay(5);
        if (Interlocked.Decrement(ref inFlightCountSignal) == 0)
        {
            tcs.TrySetResult();
        }
        await signalTask;
        stopwatchSignal.Stop();

        // Assert that signal synchronization completes much faster than polling wait (50ms quant)
        Assert.True(stopwatchSignal.ElapsedMilliseconds < stopwatchPolling.ElapsedMilliseconds,
            $"Signal elapsed ({stopwatchSignal.ElapsedMilliseconds} ms) should be faster than Polling elapsed ({stopwatchPolling.ElapsedMilliseconds} ms)");
    }
}
