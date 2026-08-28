using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Archeologist.Core;

public sealed class ProjectScanner
{
    public IReadOnlyList<Finding> Scan(string path)
    {
        var files = ResolveFiles(path);
        var findings = new List<Finding>();

        foreach (var file in files)
        {
            var text = SourceText.From(File.ReadAllText(file));
            var tree = CSharpSyntaxTree.ParseText(text, path: file);
            var walker = new CodeWalker(file, text);
            walker.Visit(tree.GetRoot());
            findings.AddRange(walker.Findings);
        }

        return findings
            .OrderBy(f => f.FilePath)
            .ThenBy(f => f.Line)
            .ToList();
    }

    private static IEnumerable<string> ResolveFiles(string path)
    {
        if (File.Exists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return [Path.GetFullPath(path)];
        }

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Yol bulunamadı: {path}");
        }

        return Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
    }
}
