using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace PromptGateway.Api;

public static partial class PiiMasker
{
    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex Email();

    [GeneratedRegex(@"\b(?:\d[ -]*?){13,19}\b")]
    private static partial Regex Card();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b")]
    private static partial Regex Ssn();

    [GeneratedRegex(@"\b(?:\+90|0)?5\d{9}\b")]
    private static partial Regex TrMobile();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b")]
    private static partial Regex IpAddress();

    [GeneratedRegex(@"(password|pwd|passwd|secret|token|api[_-]?key)\s*[:=]\s*[""']?([^""'\s,}]+)", RegexOptions.IgnoreCase)]
    private static partial Regex Credentials();

    public static string Mask(string input)
    {
        var t = Email().Replace(input, "[EMAIL]");
        t = Card().Replace(t, "[CARD]");
        t = Ssn().Replace(t, "[SSN]");
        t = TrMobile().Replace(t, "[PHONE]");
        t = IpAddress().Replace(t, "[IP]");
        t = Credentials().Replace(t, m => $"{m.Groups[1].Value}:[REDACTED]");
        return t;
    }
}

public sealed class BudgetBook
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    public long Limit { get; init; } = 8_000;
    public TimeSpan ResetInterval { get; init; } = TimeSpan.FromHours(1);

    public bool TryConsume(string key, int tokens, out long remaining)
    {
        var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket(Limit, ResetInterval));
        return bucket.TryConsume(tokens, out remaining);
    }

    public IReadOnlyDictionary<string, long> Snapshot() => 
        _buckets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Remaining);

    public void Reset(string key) => _buckets.TryRemove(key, out _);
}

public sealed class TokenBucket
{
    private long _tokens;
    private readonly long _capacity;
    private readonly TimeSpan _resetInterval;
    private DateTimeOffset _lastReset;
    private readonly object _lock = new();

    public TokenBucket(long capacity, TimeSpan resetInterval)
    {
        _capacity = capacity;
        _tokens = capacity;
        _resetInterval = resetInterval;
        _lastReset = DateTimeOffset.UtcNow;
    }

    public long Remaining => _tokens;

    public bool TryConsume(int tokens, out long remaining)
    {
        lock (_lock)
        {
            if (DateTimeOffset.UtcNow - _lastReset >= _resetInterval)
            {
                _tokens = _capacity;
                _lastReset = DateTimeOffset.UtcNow;
            }

            if (_tokens >= tokens)
            {
                _tokens -= tokens;
                remaining = _tokens;
                return true;
            }

            remaining = _tokens;
            return false;
        }
    }
}

public sealed record AuditRow(DateTimeOffset At, string Key, string Model, int Tokens, string Path, string Preview);

public sealed class AuditLog
{
    private readonly ConcurrentQueue<AuditRow> _rows = new();

    public void Add(AuditRow row)
    {
        _rows.Enqueue(row);
        while (_rows.Count > 200 && _rows.TryDequeue(out _)) { }
    }

    public void SeedDemo()
    {
        if (!_rows.IsEmpty)
        {
            return;
        }

        Add(new AuditRow(DateTimeOffset.UtcNow.AddHours(-3), "sales-desk", "company-fast", 38, "/v1/chat/completions", "Teklif özeti: müşteri [EMAIL]"));
        Add(new AuditRow(DateTimeOffset.UtcNow.AddHours(-2), "support", "company-strong", 91, "/v1/chat/completions", "Ticket #4412 — IP [IP]"));
        Add(new AuditRow(DateTimeOffset.UtcNow.AddHours(-1), "team-a", "company-strong", 54, "/v1/chat/completions", "Kart bilgisi [CARD] gizlendi"));
        Add(new AuditRow(DateTimeOffset.UtcNow.AddMinutes(-20), "hr-bot", "company-fast", 22, "/v1/chat/completions", "Aday telefon [PHONE]"));
    }
}

public sealed class ModelRouter
{
    private readonly Dictionary<string, string> _routes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gpt-4o"] = "company-strong",
        ["claude-3"] = "company-strong",
        ["gpt-4o-mini"] = "company-fast",
        ["cheap"] = "company-fast"
    };

    public string Pick(string? requested) => 
        !string.IsNullOrWhiteSpace(requested) && _routes.TryGetValue(requested, out var mapped) 
            ? mapped 
            : "company-fast";

    public void AddRoute(string alias, string target) => _routes[alias] = target;
    
    public IReadOnlyDictionary<string, string> GetRoutes() => _routes;
}
