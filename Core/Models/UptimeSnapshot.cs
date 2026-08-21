using LiteDB;
using System;

namespace NodeRadarPro.Core;

/// <summary>
/// Periodic status snapshot for uptime history charts.
/// One record per device per monitor cycle.
/// </summary>
public class UptimeSnapshot
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();

    public string MacAddress { get; set; } = "";
    public bool IsOnline { get; set; }
    public long LatencyMs { get; set; } = -1;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
