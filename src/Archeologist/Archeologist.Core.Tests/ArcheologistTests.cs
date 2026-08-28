using Archeologist.Core;

namespace Archeologist.Core.Tests;

public class ProjectScannerTests
{
    [Fact]
    public void Scan_EmptyDirectory_ReturnsEmptyList()
    {
        var scanner = new ProjectScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var findings = scanner.Scan(tempDir);
            Assert.Empty(findings);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Scan_FileWithHttpClientLeak_FindsIssue()
    {
        var scanner = new ProjectScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testFile = Path.Combine(tempDir, "test.cs");

        var code = @"
public class BadClass
{
    public void Method()
    {
        var client = new HttpClient();
        client.GetAsync(""https://api.example.com"");
    }
}";
        File.WriteAllText(testFile, code);

        try
        {
            var findings = scanner.Scan(tempDir);
            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Kind == FindingKind.HttpClientLeak);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Scan_FileWithSecretLiteral_FindsIssue()
    {
        var scanner = new ProjectScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testFile = Path.Combine(tempDir, "test.cs");

        var code = @"
public class BadClass
{
    private string apiKey = ""sk-1234567890abcdef"";
}";
        File.WriteAllText(testFile, code);

        try
        {
            var findings = scanner.Scan(tempDir);
            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Kind == FindingKind.SecretLiteral);
            Assert.Contains(findings, f => f.Severity == "CRITICAL");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Scan_FileWithSyncOverAsync_FindsIssue()
    {
        var scanner = new ProjectScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testFile = Path.Combine(tempDir, "test.cs");

        var code = @"
public class BadClass
{
    public string Method()
    {
        return GetDataAsync().Result;
    }
    
    public async Task<string> GetDataAsync() => await Task.FromResult(""data"");
}";
        File.WriteAllText(testFile, code);

        try
        {
            var findings = scanner.Scan(tempDir);
            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Kind == FindingKind.SyncOverAsync);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

public class FindingTests
{
    [Theory]
    [InlineData(FindingKind.HttpClientLeak, "HIGH")]
    [InlineData(FindingKind.SecretLiteral, "CRITICAL")]
    [InlineData(FindingKind.SyncOverAsync, "HIGH")]
    [InlineData(FindingKind.LocalDateTime, "MEDIUM")]
    [InlineData(FindingKind.StringConcatenationInLoop, "LOW")]
    public void Finding_HasCorrectSeverity(FindingKind kind, string expectedSeverity)
    {
        var finding = new Finding(kind, "test.cs", 1, 1, "test", "test", "test");
        Assert.Equal(expectedSeverity, finding.Severity);
    }
}

public class MarkdownReportTests
{
    [Fact]
    public void Render_EmptyFindings_ReturnsCleanMessage()
    {
        var findings = new List<Finding>();
        var report = MarkdownReport.Render("/test/path", findings);
        
        Assert.Contains("Temiz tarama", report);
        Assert.Contains("0", report);
    }

    [Fact]
    public void Render_WithFindings_ContainsAllSections()
    {
        var findings = new List<Finding>
        {
            new(FindingKind.HttpClientLeak, "test.cs", 1, 1, "test message", "snippet", "patch")
        };
        
        var report = MarkdownReport.Render("/test/path", findings);
        
        Assert.Contains("# Kod arkeoloğu raporu", report);
        Assert.Contains("HttpClient sızıntısı", report);
        Assert.Contains("test.cs:1:1", report);
        Assert.Contains("HIGH", report);
    }
}
