using System.Text.Json.Serialization;

namespace EventSync_Manager.Models;

public class DumpManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
    
    [JsonPropertyName("organization_id")]
    public int OrganizationId { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("events_count")]
    public int EventsCount { get; set; }
    
    [JsonPropertyName("files_count")]
    public int FilesCount { get; set; }
    
    [JsonPropertyName("checksum")]
    public string Checksum { get; set; } = string.Empty; // SHA-256 всего дампа
    
    [JsonPropertyName("signature")]
    public string? Signature { get; set; } // Цифровая подпись
}

