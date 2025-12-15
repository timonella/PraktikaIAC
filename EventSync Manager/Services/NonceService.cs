using Npgsql;

namespace EventSync_Manager.Services;

public class NonceService
{
    private readonly string _connectionString;

    public NonceService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeNonceTableAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = @"
            CREATE TABLE IF NOT EXISTS used_nonces (
                nonce VARCHAR(255) PRIMARY KEY,
                organization_id INTEGER NOT NULL,
                timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                dump_path TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_used_nonces_org_id ON used_nonces(organization_id);
            CREATE INDEX IF NOT EXISTS idx_used_nonces_timestamp ON used_nonces(timestamp);
        ";

        await using var cmd = new NpgsqlCommand(query, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> IsNonceUsedAsync(string nonce)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = "SELECT COUNT(*) FROM used_nonces WHERE nonce = @nonce";
        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("nonce", nonce);

        var count = await cmd.ExecuteScalarAsync();
        return count != null && Convert.ToInt64(count) > 0;
    }

    public async Task MarkNonceAsUsedAsync(string nonce, int organizationId, string? dumpPath = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = @"
            INSERT INTO used_nonces (nonce, organization_id, dump_path)
            VALUES (@nonce, @orgId, @dumpPath)
            ON CONFLICT (nonce) DO NOTHING";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("nonce", nonce);
        cmd.Parameters.AddWithValue("orgId", organizationId);
        cmd.Parameters.AddWithValue("dumpPath", dumpPath ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CleanupOldNoncesAsync(TimeSpan olderThan)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cutoffDate = DateTime.UtcNow - olderThan;
        var query = "DELETE FROM used_nonces WHERE timestamp < @cutoffDate";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("cutoffDate", cutoffDate);

        await cmd.ExecuteNonQueryAsync();
    }
}

