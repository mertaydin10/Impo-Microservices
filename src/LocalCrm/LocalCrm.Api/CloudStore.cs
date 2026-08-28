using System.Text.Json;
using LocalCrm.Shared;
using Microsoft.Data.Sqlite;

namespace LocalCrm.Api;

public sealed class CloudStore
{
    private readonly string _cs;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public CloudStore(string dbPath)
    {
        _cs = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        using var db = Open();
        db.Execute(@"
CREATE TABLE IF NOT EXISTS contacts (
  id TEXT PRIMARY KEY,
  payload TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);");
    }

    public IReadOnlyList<ContactRecord> All()
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT payload FROM contacts";
        using var r = cmd.ExecuteReader();
        var list = new List<ContactRecord>();
        while (r.Read())
        {
            list.Add(JsonSerializer.Deserialize<ContactRecord>(r.GetString(0), Json)!);
        }

        return list;
    }

    public ContactRecord? Get(Guid id)
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT payload FROM contacts WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        var payload = cmd.ExecuteScalar() as string;
        return payload is null ? null : JsonSerializer.Deserialize<ContactRecord>(payload, Json);
    }

    public ContactRecord Upsert(ContactRecord incoming)
    {
        var existing = Get(incoming.Id);
        var merged = existing is null ? incoming : FieldMerger.Merge(existing, incoming);
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO contacts(id, payload, updated_at) 
            VALUES ($id, $p, datetime('now')) 
            ON CONFLICT(id) DO UPDATE SET payload = $p, updated_at = datetime('now')";
        cmd.Parameters.AddWithValue("$id", merged.Id.ToString());
        cmd.Parameters.AddWithValue("$p", JsonSerializer.Serialize(merged, Json));
        cmd.ExecuteNonQuery();
        return merged;
    }

    public bool Delete(Guid id)
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM contacts WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        return cmd.ExecuteNonQuery() > 0;
    }

    public StoreStats GetStats()
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM contacts";
        var count = (long)cmd.ExecuteScalar()!;
        
        return new StoreStats(
            TotalContacts: (int)count,
            LastUpdated: DateTimeOffset.UtcNow
        );
    }

    private SqliteConnection Open()
    {
        var db = new SqliteConnection(_cs);
        db.Open();
        return db;
    }
}

public sealed record StoreStats(int TotalContacts, DateTimeOffset LastUpdated);

internal static class SqliteExt
{
    public static int Execute(this SqliteConnection db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteNonQuery();
    }
}
