namespace EventSync_Manager.Models;

public class FileAttachment
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty; // SHA-256
    public string Filepath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

