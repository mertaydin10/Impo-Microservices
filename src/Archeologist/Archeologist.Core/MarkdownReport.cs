using System.Text;

namespace Archeologist.Core;

public static class MarkdownReport
{
    public static string Render(string scannedPath, IReadOnlyList<Finding> findings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Kod arkeoloğu raporu");
        sb.AppendLine();
        sb.AppendLine($"- **Hedef:** `{scannedPath}`");
        sb.AppendLine($"- **Tarih:** {DateTimeOffset.UtcNow:u}");
        sb.AppendLine($"- **Bulgu:** {findings.Count}");
        sb.AppendLine();

        var bySeverity = findings.GroupBy(f => f.Severity).OrderByDescending(g => SeverityOrder(g.Key));
        if (bySeverity.Any())
        {
            sb.AppendLine("### Severity dağılımı");
            sb.AppendLine();
            foreach (var group in bySeverity)
            {
                var emoji = group.Key switch
                {
                    "CRITICAL" => "🔴",
                    "HIGH" => "🟠",
                    "MEDIUM" => "🟡",
                    _ => "🟢"
                };
                sb.AppendLine($"- {emoji} **{group.Key}**: {group.Count()}");
            }
            sb.AppendLine();
        }

        foreach (var group in findings.GroupBy(f => f.Kind))
        {
            var sample = group.First();
            sb.AppendLine($"## {Title(group.Key)} ({group.Count()}) [{sample.Severity}]");
            sb.AppendLine();

            foreach (var f in group)
            {
                sb.AppendLine($"### `{Path.GetFileName(f.FilePath)}:{f.Line}:{f.Column}`");
                sb.AppendLine();
                sb.AppendLine(f.Message);
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine(f.Snippet);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("**Önerilen patch**");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine(f.SuggestedPatch);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine($"Dosya: `{f.FilePath}`");
                sb.AppendLine();
            }
        }

        if (findings.Count == 0)
        {
            sb.AppendLine("_Temiz tarama. Bilinen anti-pattern bulunamadı._");
        }

        return sb.ToString();
    }

    private static int SeverityOrder(string severity) => severity switch
    {
        "CRITICAL" => 4,
        "HIGH" => 3,
        "MEDIUM" => 2,
        "LOW" => 1,
        _ => 0
    };

    private static string Title(FindingKind kind) => kind switch
    {
        FindingKind.HttpClientLeak => "HttpClient sızıntısı",
        FindingKind.SyncOverAsync => "Senkron-over-async",
        FindingKind.LocalDateTime => "DateTime.Now / yerel saat",
        FindingKind.SecretLiteral => "Gizli string",
        FindingKind.SqlInjectionRisk => "SQL injection riski",
        FindingKind.EmptyCatchBlock => "Boş catch bloğu",
        FindingKind.StringConcatenationInLoop => "Döngüde string concat",
        FindingKind.ThreadSleep => "Thread.Sleep",
        _ => kind.ToString()
    };
}
