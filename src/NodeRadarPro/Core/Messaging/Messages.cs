using System.Collections.Generic;

namespace NodeRadarPro.Core.Messaging;

public record GlobalStatsUpdatedMessage(int OnlineCount, long AverageLatency, int UnresolvedAlerts);
public record NodesUpdatedMessage(IReadOnlyList<NetworkNode> Nodes);
