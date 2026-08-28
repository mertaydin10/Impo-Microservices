using System.CommandLine;
using System.Text.Json;
using Archeologist.Core;

var pathArg = new Argument<string>("path")
{
    Description = "Taranacak klasör veya .cs dosyası"
};
var outputOpt = new Option<string>("--output", "-o")
{
    Description = "Markdown rapor dosyası (varsayılan: stdout)"
};
var jsonOpt = new Option<bool>("--json", "-j")
{
    Description = "JSON formatında çıktı"
};
var severityOpt = new Option<string?>("--min-severity", "-s")
{
    Description = "Minimum severity: LOW, MEDIUM, HIGH, CRITICAL"
};

var root = new RootCommand("Kod arkeoloğu — eski .NET kodunda HttpClient, sync-over-async, DateTime.Now ve sır sızıntısı tarar.")
{
    pathArg,
    outputOpt,
    jsonOpt,
    severityOpt
};

root.SetAction(parseResult =>
{
    var path = parseResult.GetValue(pathArg)!;
    var output = parseResult.GetValue(outputOpt);
    var json = parseResult.GetValue(jsonOpt);
    var minSeverity = parseResult.GetValue(severityOpt);

    var scanner = new ProjectScanner();
    var findings = scanner.Scan(path);

    if (!string.IsNullOrWhiteSpace(minSeverity))
    {
        var severityOrder = new[] { "LOW", "MEDIUM", "HIGH", "CRITICAL" };
        var minIndex = Array.IndexOf(severityOrder, minSeverity.ToUpperInvariant());
        if (minIndex >= 0)
        {
            findings = findings.Where(f => Array.IndexOf(severityOrder, f.Severity) >= minIndex).ToList();
        }
    }

    string content;
    if (json)
    {
        var result = new
        {
            scannedPath = Path.GetFullPath(path),
            totalFindings = findings.Count,
            findingsBySeverity = findings.GroupBy(f => f.Severity).ToDictionary(g => g.Key, g => g.Count()),
            findings = findings.Select(f => new
            {
                kind = f.Kind.ToString(),
                severity = f.Severity,
                file = f.FilePath,
                line = f.Line,
                column = f.Column,
                message = f.Message,
                snippet = f.Snippet,
                suggestedFix = f.SuggestedPatch
            })
        };
        content = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
    else
    {
        content = MarkdownReport.Render(Path.GetFullPath(path), findings);
    }

    if (string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine(content);
    }
    else
    {
        File.WriteAllText(output, content);
        Console.WriteLine($"Rapor yazıldı: {output} ({findings.Count} bulgu)");
    }

    return findings.Count == 0 ? 0 : 1;
});

return root.Parse(args).Invoke();
