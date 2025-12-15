using EventSync_Manager.Models;
using Npgsql;

namespace EventSync_Manager.Services;

public class EventLogService
{
    private readonly string _connectionString;
    private const int BatchSize = 100; // Батчинг для массовой записи логов

    public EventLogService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task LogEventChangeAsync(
        int eventId,
        string? statusOld,
        string? statusNew,
        string? comment,
        string? source = "manager",
        string action = "update",
        string? userName = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = @"
            INSERT INTO event_logs (event_id, timestamp, status_old, status_new, comment, user_name, action, source)
            VALUES (@eventId, @timestamp, @statusOld, @statusNew, @comment, @userName, @action, @source)";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("eventId", eventId);
        cmd.Parameters.AddWithValue("timestamp", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("statusOld", statusOld ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("statusNew", statusNew ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("comment", comment ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("userName", userName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("action", action);
        cmd.Parameters.AddWithValue("source", source ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task LogBatchChangesAsync(List<EventLog> logs)
    {
        if (logs.Count == 0) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Оптимизированная массовая вставка через COPY
        await using var writer = await conn.BeginBinaryImportAsync(@"
            COPY event_logs (event_id, timestamp, status_old, status_new, comment, user_name, action, source)
            FROM STDIN (FORMAT BINARY)");

        foreach (var log in logs)
        {
            await writer.StartRowAsync();
            await writer.WriteAsync(log.EventId);
            await writer.WriteAsync(log.Timestamp);
            await writer.WriteAsync(log.StatusOld ?? (object)DBNull.Value);
            await writer.WriteAsync(log.StatusNew ?? (object)DBNull.Value);
            await writer.WriteAsync(log.Comment ?? (object)DBNull.Value);
            await writer.WriteAsync(log.User ?? (object)DBNull.Value);
            await writer.WriteAsync(log.Action);
            await writer.WriteAsync(log.Source ?? (object)DBNull.Value);
        }

        await writer.CompleteAsync();
    }

    public async Task<List<EventLog>> GetEventLogsAsync(
        int? eventId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? source = null,
        int limit = 1000)
    {
        var logs = new List<EventLog>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = @"
            SELECT id, event_id, timestamp, status_old, status_new, comment, user_name, action, source
            FROM event_logs
            WHERE 1=1";

        var parameters = new List<NpgsqlParameter>();

        if (eventId.HasValue)
        {
            query += " AND event_id = @eventId";
            parameters.Add(new NpgsqlParameter("eventId", eventId.Value));
        }

        if (fromDate.HasValue)
        {
            query += " AND timestamp >= @fromDate";
            parameters.Add(new NpgsqlParameter("fromDate", fromDate.Value));
        }

        if (toDate.HasValue)
        {
            query += " AND timestamp <= @toDate";
            parameters.Add(new NpgsqlParameter("toDate", toDate.Value));
        }

        if (!string.IsNullOrEmpty(source))
        {
            query += " AND source = @source";
            parameters.Add(new NpgsqlParameter("source", source));
        }

        query += " ORDER BY timestamp DESC LIMIT @limit";
        parameters.Add(new NpgsqlParameter("limit", limit));

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddRange(parameters.ToArray());

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new EventLog
            {
                Id = reader.GetInt32(0),
                EventId = reader.GetInt32(1),
                Timestamp = reader.GetDateTime(2),
                StatusOld = reader.IsDBNull(3) ? null : reader.GetString(3),
                StatusNew = reader.IsDBNull(4) ? null : reader.GetString(4),
                Comment = reader.IsDBNull(5) ? null : reader.GetString(5),
                User = reader.IsDBNull(6) ? null : reader.GetString(6),
                Action = reader.GetString(7),
                Source = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return logs;
    }

    public async Task<List<EventLog>> GetEventHistoryAsync(int eventId)
    {
        return await GetEventLogsAsync(eventId: eventId, limit: 100);
    }

    public async Task<Dictionary<int, EventLog?>> GetLastStatusChangeAsync(List<int> eventIds)
    {
        var result = new Dictionary<int, EventLog?>();
        if (eventIds.Count == 0) return result;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = @"
            SELECT DISTINCT ON (event_id)
                id, event_id, timestamp, status_old, status_new, comment, user_name, action, source
            FROM event_logs
            WHERE event_id = ANY(@eventIds) AND status_new IS NOT NULL
            ORDER BY event_id, timestamp DESC";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("eventIds", eventIds.ToArray());

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var log = new EventLog
            {
                Id = reader.GetInt32(0),
                EventId = reader.GetInt32(1),
                Timestamp = reader.GetDateTime(2),
                StatusOld = reader.IsDBNull(3) ? null : reader.GetString(3),
                StatusNew = reader.IsDBNull(4) ? null : reader.GetString(4),
                Comment = reader.IsDBNull(5) ? null : reader.GetString(5),
                User = reader.IsDBNull(6) ? null : reader.GetString(6),
                Action = reader.GetString(7),
                Source = reader.IsDBNull(8) ? null : reader.GetString(8)
            };
            result[log.EventId] = log;
        }

        return result;
    }
}

