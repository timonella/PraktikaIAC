using Npgsql;
using EventSync_Manager.Models;

namespace EventSync_Manager.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Создание таблиц
        var createTables = @"
            CREATE TABLE IF NOT EXISTS organizations (
                id SERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                inn VARCHAR(20) UNIQUE NOT NULL,
                address TEXT,
                contact_person VARCHAR(255),
                encryption_key BYTEA NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS events (
                id SERIAL PRIMARY KEY,
                title VARCHAR(255) NOT NULL,
                start_date TIMESTAMP NOT NULL,
                due_date TIMESTAMP,
                control_date TIMESTAMP,
                status VARCHAR(50) DEFAULT 'planned',
                description TEXT,
                organization_id INTEGER REFERENCES organizations(id) ON DELETE CASCADE,
                location VARCHAR(255),
                priority VARCHAR(20) DEFAULT 'normal',
                responsible_person VARCHAR(255),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP,
                version BIGINT DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS event_logs (
                id SERIAL PRIMARY KEY,
                event_id INTEGER REFERENCES events(id) ON DELETE CASCADE,
                timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                status_old VARCHAR(50),
                status_new VARCHAR(50),
                comment TEXT,
                user_name VARCHAR(255),
                action VARCHAR(20) DEFAULT 'update',
                source VARCHAR(20)
            );

            CREATE TABLE IF NOT EXISTS file_attachments (
                id SERIAL PRIMARY KEY,
                event_id INTEGER REFERENCES events(id) ON DELETE CASCADE,
                filename VARCHAR(255) NOT NULL,
                hash VARCHAR(64) NOT NULL,
                filepath TEXT NOT NULL,
                file_size BIGINT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS idx_events_org_id ON events(organization_id);
            CREATE INDEX IF NOT EXISTS idx_events_status ON events(status);
            CREATE INDEX IF NOT EXISTS idx_event_logs_event_id ON event_logs(event_id);
            CREATE INDEX IF NOT EXISTS idx_file_attachments_event_id ON file_attachments(event_id);
        ";

        await using var cmd = new NpgsqlCommand(createTables, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Event>> GetEventsForOrganizationAsync(int organizationId, DateTime? since = null)
    {
        var events = new List<Event>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = @"
            SELECT id, title, start_date, due_date, control_date, status, description,
                   organization_id, location, priority, responsible_person, created_at, updated_at, version
            FROM events
            WHERE organization_id = @orgId";

        if (since.HasValue)
        {
            query += " AND (updated_at >= @since OR created_at >= @since)";
        }

        query += " ORDER BY id";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("orgId", organizationId);
        if (since.HasValue)
        {
            cmd.Parameters.AddWithValue("since", since.Value);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            events.Add(new Event
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
            });
        }

        return events;
    }

    public async Task<List<FileAttachment>> GetFileAttachmentsForEventsAsync(List<int> eventIds)
    {
        if (eventIds.Count == 0) return new List<FileAttachment>();

        var attachments = new List<FileAttachment>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = @"
            SELECT id, event_id, filename, hash, filepath, file_size, created_at
            FROM file_attachments
            WHERE event_id = ANY(@eventIds)";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("eventIds", eventIds.ToArray());

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            attachments.Add(new FileAttachment
            {
                Id = reader.GetInt32(0),
                EventId = reader.GetInt32(1),
                Filename = reader.GetString(2),
                Hash = reader.GetString(3),
                Filepath = reader.GetString(4),
                FileSize = reader.GetInt64(5),
                CreatedAt = reader.GetDateTime(6)
            });
        }

        return attachments;
    }

    public async Task<Organization?> GetOrganizationAsync(int organizationId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = @"
            SELECT id, name, inn, address, contact_person, encryption_key, created_at, updated_at
            FROM organizations
            WHERE id = @id";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("id", organizationId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Organization
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Inn = reader.GetString(2),
                Address = reader.IsDBNull(3) ? null : reader.GetString(3),
                ContactPerson = reader.IsDBNull(4) ? null : reader.GetString(4),
                EncryptionKey = (byte[])reader[5],
                CreatedAt = reader.GetDateTime(6),
                UpdatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            };
        }

        return null;
    }
}

