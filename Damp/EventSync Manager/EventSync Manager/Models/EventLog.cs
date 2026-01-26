namespace EventSync_Manager.Models;

public class EventLog
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? StatusOld { get; set; }
    public string? StatusNew { get; set; }
    public string? Comment { get; set; }
    public string? User { get; set; }
    public string Action { get; set; } = "update"; // create, update, delete
    public string? Source { get; set; } // "manager" или "field"
}

