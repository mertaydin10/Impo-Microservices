namespace Archeologist.Core;

public enum FindingKind
{
    HttpClientLeak,
    SyncOverAsync,
    LocalDateTime,
    SecretLiteral,
    StringConcatenationInLoop,
    EmptyCatchBlock,
    SqlInjectionRisk,
    ThreadSleep
}

public sealed record Finding(
    FindingKind Kind,
    string FilePath,
    int Line,
    int Column,
    string Message,
    string Snippet,
    string SuggestedPatch)
{
    public string Severity => Kind switch
    {
        FindingKind.HttpClientLeak => "HIGH",
        FindingKind.SyncOverAsync => "HIGH",
        FindingKind.SecretLiteral => "CRITICAL",
        FindingKind.SqlInjectionRisk => "CRITICAL",
        FindingKind.LocalDateTime => "MEDIUM",
        FindingKind.ThreadSleep => "MEDIUM",
        FindingKind.StringConcatenationInLoop => "LOW",
        FindingKind.EmptyCatchBlock => "MEDIUM",
        _ => "LOW"
    };
}
