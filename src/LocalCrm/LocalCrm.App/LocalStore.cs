using System.Text.Json;
using LocalCrm.Shared;
using Microsoft.Data.Sqlite;

namespace LocalCrm.App;

public sealed class LocalStore
{
    private readonly string _cs;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public LocalStore(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _cs = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS contacts (id TEXT PRIMARY KEY, payload TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS meta (k TEXT PRIMARY KEY, v TEXT NOT NULL);
            """;
        cmd.ExecuteNonQuery();
    }

    public string DeviceId
    {
        get
        {
            using var db = Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT v FROM meta WHERE k = 'device'";
            if (cmd.ExecuteScalar() is string v)
            {
                return v;
            }

            var id = "device-" + Guid.NewGuid().ToString("N")[..6];
            using var ins = db.CreateCommand();
            ins.CommandText = "INSERT INTO meta(k,v) VALUES ('device', $v)";
            ins.Parameters.AddWithValue("$v", id);
            ins.ExecuteNonQuery();
            return id;
        }
    }

    public IReadOnlyList<ContactRecord> All(bool includeDeleted = false)
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

        return list
            .Where(c => includeDeleted || c.DeletedAt == default)
            .OrderBy(c => c.Name.Value)
            .ToList();
    }

    public void Save(ContactRecord contact)
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO contacts(id, payload) VALUES ($id, $p) ON CONFLICT(id) DO UPDATE SET payload = $p";
        cmd.Parameters.AddWithValue("$id", contact.Id.ToString());
        cmd.Parameters.AddWithValue("$p", JsonSerializer.Serialize(contact, Json));
        cmd.ExecuteNonQuery();
    }

    public HashSet<Guid> GetFavorites()
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT v FROM meta WHERE k = 'favorites'";
        if (cmd.ExecuteScalar() is not string raw || string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(raw)?.ToHashSet() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void SetFavorite(Guid id, bool favorite)
    {
        var favorites = GetFavorites();
        if (favorite)
        {
            favorites.Add(id);
        }
        else
        {
            favorites.Remove(id);
        }

        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO meta(k,v) VALUES ('favorites', $v) ON CONFLICT(k) DO UPDATE SET v = $v";
        cmd.Parameters.AddWithValue("$v", JsonSerializer.Serialize(favorites.ToList()));
        cmd.ExecuteNonQuery();
    }

    public DateTimeOffset? GetLastSyncAt()
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT v FROM meta WHERE k = 'last_sync_at'";
        return cmd.ExecuteScalar() is string raw && DateTimeOffset.TryParse(raw, out var at) ? at : null;
    }

    public void SetLastSyncAt(DateTimeOffset at)
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO meta(k,v) VALUES ('last_sync_at', $v) ON CONFLICT(k) DO UPDATE SET v = $v";
        cmd.Parameters.AddWithValue("$v", at.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void SeedIfEmpty()
    {
        if (All().Count > 0)
        {
            return;
        }

        foreach (var c in CrmDemoContacts.Create(DeviceId))
        {
            Save(c);
        }
    }

    public void ReplaceAll(IEnumerable<ContactRecord> contacts)
    {
        using var db = Open();
        using var tx = db.BeginTransaction();
        using (var del = db.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM contacts";
            del.ExecuteNonQuery();
        }

        foreach (var c in contacts)
        {
            using var cmd = db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO contacts(id, payload) VALUES ($id, $p)";
            cmd.Parameters.AddWithValue("$id", c.Id.ToString());
            cmd.Parameters.AddWithValue("$p", JsonSerializer.Serialize(c, Json));
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private SqliteConnection Open()
    {
        var db = new SqliteConnection(_cs);
        db.Open();
        return db;
    }
}
