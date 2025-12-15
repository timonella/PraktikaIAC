using EventSync_Manager.Models;
using Npgsql;

namespace EventSync_Manager.Services;

public class ConflictResolutionService
{
    private readonly string _connectionString;
    private readonly EventLogService _eventLogService;

    public ConflictResolutionService(string connectionString, EventLogService eventLogService)
    {
        _connectionString = connectionString;
        _eventLogService = eventLogService;
    }

    public enum ConflictResolutionStrategy
    {
        ServerWins,      // Версия сервера (Manager) имеет приоритет
        ClientWins,      // Версия клиента (Field) имеет приоритет
        LastWriteWins,   // Побеждает последняя запись по timestamp
        Merge,           // Слияние изменений
        Manual           // Требуется ручное разрешение
    }

    public async Task<MergeResult> MergeEventsAsync(
        List<Event> incomingEvents,
        int organizationId,
        ConflictResolutionStrategy strategy = ConflictResolutionStrategy.LastWriteWins)
    {
        var result = new MergeResult
        {
            Created = 0,
            Updated = 0,
            Conflicts = new List<ConflictInfo>(),
            Skipped = 0
        };

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        foreach (var incomingEvent in incomingEvents)
        {
            // Проверяем существование события
            var existingEvent = await GetEventByIdAsync(conn, incomingEvent.Id);

            if (existingEvent == null)
            {
                // Новое событие - создаём
                await CreateEventAsync(conn, incomingEvent, organizationId);
                await _eventLogService.LogEventChangeAsync(
                    incomingEvent.Id,
                    null,
                    incomingEvent.Status,
                    "Событие создано при синхронизации",
                    "field",
                    "create"
                );
                result.Created++;
            }
            else
            {
                // Существующее событие - проверяем конфликты
                var conflict = DetectConflict(existingEvent, incomingEvent);

                if (conflict.HasConflict)
                {
                    var resolution = await ResolveConflictAsync(
                        existingEvent,
                        incomingEvent,
                        conflict,
                        strategy
                    );

                    if (resolution.Resolved)
                    {
                        await UpdateEventAsync(conn, resolution.ResolvedEvent!);
                        await _eventLogService.LogEventChangeAsync(
                            resolution.ResolvedEvent!.Id,
                            existingEvent.Status,
                            resolution.ResolvedEvent.Status,
                            $"Конфликт разрешён: {resolution.ResolutionNote}",
                            "field",
                            "update"
                        );
                        result.Updated++;
                    }
                    else
                    {
                        result.Conflicts.Add(conflict);
                        result.Skipped++;
                    }
                }
                else
                {
                    // Нет конфликта - просто обновляем если версия новее
                    if (incomingEvent.Version > existingEvent.Version)
                    {
                        await UpdateEventAsync(conn, incomingEvent);
                        await _eventLogService.LogEventChangeAsync(
                            incomingEvent.Id,
                            existingEvent.Status,
                            incomingEvent.Status,
                            "Обновление при синхронизации",
                            "field",
                            "update"
                        );
                        result.Updated++;
                    }
                    else
                    {
                        result.Skipped++;
                    }
                }
            }
        }

        return result;
    }

    private async Task<Event?> GetEventByIdAsync(NpgsqlConnection conn, int eventId)
    {
        var query = @"
            SELECT id, title, start_date, due_date, control_date, status, description,
                   organization_id, location, priority, responsible_person, created_at, updated_at, version
            FROM events
            WHERE id = @id";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("id", eventId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Event
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                StartDate = reader.GetDateTime(2),
                DueDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                ControlDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Status = reader.GetString(5),
                Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                OrganizationId = reader.GetInt32(7),
                Location = reader.IsDBNull(8) ? null : reader.GetString(8),
                Priority = reader.GetString(9),
                ResponsiblePerson = reader.IsDBNull(10) ? null : reader.GetString(10),
                CreatedAt = reader.GetDateTime(11),
                UpdatedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                Version = reader.GetInt64(13)
            };
        }

        return null;
    }

    private ConflictInfo DetectConflict(Event existing, Event incoming)
    {
        var conflict = new ConflictInfo
        {
            EventId = existing.Id,
            HasConflict = false,
            ConflictFields = new List<string>()
        };

        // Проверяем изменения в критических полях
        if (existing.Version == incoming.Version && 
            (existing.UpdatedAt.HasValue && incoming.UpdatedAt.HasValue) &&
            existing.UpdatedAt.Value < incoming.UpdatedAt.Value)
        {
            // Одинаковая версия, но разные timestamps - возможен конфликт
            if (existing.Status != incoming.Status)
            {
                conflict.HasConflict = true;
                conflict.ConflictFields.Add("status");
            }

            if (existing.Description != incoming.Description)
            {
                conflict.HasConflict = true;
                conflict.ConflictFields.Add("description");
            }

            if (existing.DueDate != incoming.DueDate)
            {
                conflict.HasConflict = true;
                conflict.ConflictFields.Add("dueDate");
            }
        }
        else if (incoming.Version <= existing.Version)
        {
            // Входящая версия старше или равна - пропускаем
            conflict.HasConflict = false;
        }

        return conflict;
    }

    private async Task<ConflictResolution> ResolveConflictAsync(
        Event existing,
        Event incoming,
        ConflictInfo conflict,
        ConflictResolutionStrategy strategy)
    {
        var resolution = new ConflictResolution
        {
            Resolved = false,
            ResolutionNote = string.Empty
        };

        switch (strategy)
        {
            case ConflictResolutionStrategy.ServerWins:
                // Оставляем существующую версию
                resolution.Resolved = false;
                resolution.ResolutionNote = "Приоритет сервера - изменения отклонены";
                break;

            case ConflictResolutionStrategy.ClientWins:
                // Принимаем входящую версию
                incoming.Version = existing.Version + 1;
                incoming.UpdatedAt = DateTime.UtcNow;
                resolution.Resolved = true;
                resolution.ResolvedEvent = incoming;
                resolution.ResolutionNote = "Приоритет клиента - изменения приняты";
                break;

            case ConflictResolutionStrategy.LastWriteWins:
                // Побеждает последняя запись
                if (incoming.UpdatedAt.HasValue && existing.UpdatedAt.HasValue &&
                    incoming.UpdatedAt.Value > existing.UpdatedAt.Value)
                {
                    incoming.Version = existing.Version + 1;
                    resolution.Resolved = true;
                    resolution.ResolvedEvent = incoming;
                    resolution.ResolutionNote = "Принята последняя версия по timestamp";
                }
                else
                {
                    resolution.Resolved = false;
                    resolution.ResolutionNote = "Существующая версия новее";
                }
                break;

            case ConflictResolutionStrategy.Merge:
                // Слияние изменений
                var merged = MergeEventFields(existing, incoming, conflict);
                merged.Version = existing.Version + 1;
                merged.UpdatedAt = DateTime.UtcNow;
                resolution.Resolved = true;
                resolution.ResolvedEvent = merged;
                resolution.ResolutionNote = "Выполнено слияние изменений";
                break;

            case ConflictResolutionStrategy.Manual:
                // Требуется ручное разрешение
                resolution.Resolved = false;
                resolution.ResolutionNote = "Требуется ручное разрешение конфликта";
                break;
        }

        return resolution;
    }

    private Event MergeEventFields(Event existing, Event incoming, ConflictInfo conflict)
    {
        var merged = new Event
        {
            Id = existing.Id,
            Title = existing.Title, // Заголовок не конфликтует обычно
            StartDate = existing.StartDate,
            OrganizationId = existing.OrganizationId,
            CreatedAt = existing.CreatedAt,
            Version = existing.Version
        };

        // Слияние полей: приоритет входящим изменениям для конфликтующих полей
        if (conflict.ConflictFields.Contains("status"))
            merged.Status = incoming.Status;
        else
            merged.Status = existing.Status;

        if (conflict.ConflictFields.Contains("description"))
            merged.Description = incoming.Description ?? existing.Description;
        else
            merged.Description = existing.Description;

        if (conflict.ConflictFields.Contains("dueDate"))
            merged.DueDate = incoming.DueDate ?? existing.DueDate;
        else
            merged.DueDate = existing.DueDate;

        // Неконфликтующие поля берём из входящих данных
        merged.ControlDate = incoming.ControlDate ?? existing.ControlDate;
        merged.Location = incoming.Location ?? existing.Location;
        merged.Priority = incoming.Priority;
        merged.ResponsiblePerson = incoming.ResponsiblePerson ?? existing.ResponsiblePerson;

        return merged;
    }

    private async Task CreateEventAsync(NpgsqlConnection conn, Event evt, int organizationId)
    {
        var query = @"
            INSERT INTO events (title, start_date, due_date, control_date, status, description,
                              organization_id, location, priority, responsible_person, version, created_at, updated_at)
            VALUES (@title, @startDate, @dueDate, @controlDate, @status, @description,
                    @orgId, @location, @priority, @responsible, @version, @createdAt, @updatedAt)
            RETURNING id";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("title", evt.Title);
        cmd.Parameters.AddWithValue("startDate", evt.StartDate);
        cmd.Parameters.AddWithValue("dueDate", evt.DueDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("controlDate", evt.ControlDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("status", evt.Status);
        cmd.Parameters.AddWithValue("description", evt.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("orgId", organizationId);
        cmd.Parameters.AddWithValue("location", evt.Location ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("priority", evt.Priority);
        cmd.Parameters.AddWithValue("responsible", evt.ResponsiblePerson ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("version", evt.Version);
        cmd.Parameters.AddWithValue("createdAt", evt.CreatedAt);
        cmd.Parameters.AddWithValue("updatedAt", evt.UpdatedAt ?? (object)DBNull.Value);

        var newId = await cmd.ExecuteScalarAsync();
        if (newId != null)
        {
            evt.Id = Convert.ToInt32(newId);
        }
    }

    private async Task UpdateEventAsync(NpgsqlConnection conn, Event evt)
    {
        var query = @"
            UPDATE events
            SET title = @title, start_date = @startDate, due_date = @dueDate,
                control_date = @controlDate, status = @status, description = @description,
                location = @location, priority = @priority, responsible_person = @responsible,
                version = @version, updated_at = @updatedAt
            WHERE id = @id";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("id", evt.Id);
        cmd.Parameters.AddWithValue("title", evt.Title);
        cmd.Parameters.AddWithValue("startDate", evt.StartDate);
        cmd.Parameters.AddWithValue("dueDate", evt.DueDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("controlDate", evt.ControlDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("status", evt.Status);
        cmd.Parameters.AddWithValue("description", evt.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("location", evt.Location ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("priority", evt.Priority);
        cmd.Parameters.AddWithValue("responsible", evt.ResponsiblePerson ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("version", evt.Version);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }
}

public class MergeResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<ConflictInfo> Conflicts { get; set; } = new();
}

public class ConflictInfo
{
    public int EventId { get; set; }
    public bool HasConflict { get; set; }
    public List<string> ConflictFields { get; set; } = new();
    public string? Description { get; set; }
}

public class ConflictResolution
{
    public bool Resolved { get; set; }
    public Event? ResolvedEvent { get; set; }
    public string ResolutionNote { get; set; } = string.Empty;
}

