using System.Net.Http.Json;
using LocalCrm.Shared;

namespace LocalCrm.App;

public sealed class SyncClient(HttpClient http, LocalStore store)
{
    public async Task<string> SyncAsync(CancellationToken ct = default)
    {
        var local = store.All(includeDeleted: true).ToList();
        var cloud = await http.GetFromJsonAsync<List<ContactRecord>>("/contacts", ct) ?? [];

        var byId = local.ToDictionary(c => c.Id);
        foreach (var remote in cloud)
        {
            if (byId.TryGetValue(remote.Id, out var existing))
            {
                byId[remote.Id] = FieldMerger.Merge(existing, remote);
            }
            else
            {
                byId[remote.Id] = remote;
            }
        }

        var merged = byId.Values.ToList();
        var pushed = await http.PostAsJsonAsync("/sync", new { contacts = merged }, ct);
        pushed.EnsureSuccessStatusCode();
        var roundtrip = await pushed.Content.ReadFromJsonAsync<SyncEnvelope>(ct);
        var saved = (roundtrip?.Contacts ?? merged).Where(c => c.DeletedAt == default).ToList();
        store.ReplaceAll(roundtrip?.Contacts ?? merged);
        var extra = roundtrip?.Errors is { Count: > 0 } ? $" Uyarı: {string.Join("; ", roundtrip.Errors)}" : "";
        return $"{saved.Count} kayıt senkron (alan bazlı birleştirme).{extra}";
    }

    private sealed record SyncEnvelope(List<ContactRecord> Contacts, List<string>? Errors);
}
