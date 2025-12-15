namespace EventSync_Manager.Models;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ControlDate { get; set; }
    public string Status { get; set; } = "planned";
    public string? Description { get; set; }
    public int OrganizationId { get; set; }
    public string? Location { get; set; }
    public string Priority { get; set; } = "normal";
    public string? ResponsiblePerson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public long Version { get; set; } = 1; // Для отслеживания версий при синхронизации
}

