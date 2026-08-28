using System.Collections.Generic;

namespace NodeRadarPro.Core.Messaging;

public record GlobalStatsUpdatedMessage(int OnlineCount, long AverageLatency, int UnresolvedAlerts);
public record NavigateToPageMessage(string PageName, string? TargetMacAddress = null);
public record NodesUpdatedMessage(IReadOnlyList<NetworkNode> Nodes);
