using LiteDB;
using System;

namespace NodeRadarPro.Core;

/// <summary>
/// System log entry for the event log viewer.
/// </summary>
public class LogEntry
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogLevel Level { get; set; } = LogLevel.Info;
    public string Source { get; set; } = "";
    public string Message { get; set; } = "";
    public string? DeviceMac { get; set; }
}

public enum LogLevel
{
    Info,
    Warning,
    Error
}
