using LocalCrm.Shared;

namespace LocalCrm.Shared;

public static class CrmDemoContacts
{
    public static IReadOnlyList<ContactRecord> Create(string deviceId, DateTimeOffset? now = null)
    {
        var t = now ?? DateTimeOffset.UtcNow;

        return
        [
            Contact("11111111-1111-1111-1111-111111111111", deviceId, t.AddDays(-12),
                "Elif Kaya", "elif.kaya@novatech.example", "05321234567", "NovaTech", "Kurumsal lisans yenileme — Q3"),
            Contact("22222222-2222-2222-2222-222222222222", deviceId, t.AddDays(-9),
                "Mert Demir", "mert.demir@maviada.example", "05449887766", "Mavi Ada Yazılım", "On-prem .NET 8 geçişi"),
            Contact("33333333-3333-3333-3333-333333333333", deviceId, t.AddDays(-6),
                "Selin Aksoy", "selin.aksoy@kuzeylo.example", "05335551212", "Kuzey Lojistik", "WMS entegrasyonu teklifi"),
            Contact("44444444-4444-4444-4444-444444444444", deviceId, t.AddDays(-4),
                "Can Öztürk", "can.ozturk@isikbank.example", "05051112233", "Işık Finans", "API gateway POC"),
            Contact("55555555-5555-5555-5555-555555555555", deviceId, t.AddDays(-2),
                "Zeynep Arslan", "zeynep.arslan@atelier.example", "05326667788", "Atelier Restoran", "Online sipariş + kurye"),
            Contact("66666666-6666-6666-6666-666666666666", deviceId, t.AddDays(-1),
                "Hakan Yıldız", "hakan.yildiz@orbit.example", "05423334455", "Orbit Medya", "Canlı skor widget (Spectator)")
        ];
    }

    private static ContactRecord Contact(
        string id, string device, DateTimeOffset at,
        string name, string email, string phone, string company, string notes) => new()
    {
        Id = Guid.Parse(id),
        Name = Clock(name, at, device),
        Email = Clock(email, at.AddHours(1), device),
        Phone = Clock(phone, at.AddHours(2), device),
        Company = Clock(company, at.AddHours(3), device),
        Notes = Clock(notes, at.AddHours(4), device)
    };

    private static FieldClock Clock(string value, DateTimeOffset at, string device) => new()
    {
        Value = value,
        UpdatedAt = at,
        DeviceId = device
    };
}
