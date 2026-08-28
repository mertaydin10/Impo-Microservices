using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Archeologist.Core;

internal sealed class CodeWalker : CSharpSyntaxWalker
{
    private readonly string _path;
    private readonly SourceText _text;
    private readonly List<Finding> _findings = [];

    public CodeWalker(string path, SourceText text)
        : base(SyntaxWalkerDepth.Trivia)
    {
        _path = path;
        _text = text;
    }

    public IReadOnlyList<Finding> Findings => _findings;

    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        if (IsHttpClientType(node.Type) && !IsDisposedWithUsing(node))
        {
            Add(FindingKind.HttpClientLeak, node,
                "HttpClient new ile oluşturulmuş ve using ile sarılmamış. Socket tükenmesi (SNAT) riski.",
                """
                // Önerilen: IHttpClientFactory veya tek paylaşılan instance
                private static readonly HttpClient Http = new();
                // veya
                await using var client = _factory.CreateClient();
                """);
        }

        base.VisitObjectCreationExpression(node);
    }

    public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
    {
        if (node.Parent is VariableDeclarationSyntax vd && IsHttpClientType(vd.Type) && !IsDisposedWithUsing(node))
        {
            Add(FindingKind.HttpClientLeak, node,
                "HttpClient (target-typed new) using dışında.",
                "using var client = new HttpClient();");
        }

        base.VisitImplicitObjectCreationExpression(node);
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var name = node.Name.Identifier.ValueText;

        if (name is "Result" or "Wait")
        {
            Add(FindingKind.SyncOverAsync, node,
                $".{name} senkron bloklama: thread pool starvation / deadlock (özellikle ASP.NET klasik).",
                "await theTask;  // .Result / .Wait() yerine");
        }

        if (name == "Now" && node.Expression.ToString() is "DateTime" or "System.DateTime" or "DateTimeOffset" or "System.DateTimeOffset")
        {
            Add(FindingKind.LocalDateTime, node,
                "Yerel saat kullanılıyor. Sunucu TZ değişince kayıtlar kayar.",
                "DateTime.UtcNow  // veya DateTimeOffset.UtcNow");
        }

        base.VisitMemberAccessExpression(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax ma &&
            ma.Name.Identifier.ValueText == "GetResult" &&
            ma.Expression is InvocationExpressionSyntax inner &&
            inner.Expression is MemberAccessExpressionSyntax ga &&
            ga.Name.Identifier.ValueText == "GetAwaiter")
        {
            Add(FindingKind.SyncOverAsync, node,
                "GetAwaiter().GetResult() senkron-over-async.",
                "await ...;");
        }

        base.VisitInvocationExpression(node);
    }

    public override void VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (!node.IsKind(SyntaxKind.StringLiteralExpression))
        {
            base.VisitLiteralExpression(node);
            return;
        }

        var value = node.Token.ValueText;
        if (LooksLikeSecret(value, node))
        {
            Add(FindingKind.SecretLiteral, node,
                "Muhtemel gizli değer kaynak kodunda. Vault / User Secrets / env kullan.",
                """
                var key = builder.Configuration["ApiKey"];
                // veya Environment.GetEnvironmentVariable("API_KEY")
                """);
        }

        if (LooksLikeSqlInjection(value, node))
        {
            Add(FindingKind.SqlInjectionRisk, node,
                "SQL string concat ile oluşturuluyor. SQL injection riski.",
                "// Parametreli sorgu kullan: new SqlCommand(\"SELECT * FROM Users WHERE Id = @id\", conn)");
        }

        base.VisitLiteralExpression(node);
    }

    public override void VisitCatchClause(CatchClauseSyntax node)
    {
        if (node.Block.Statements.Count == 0 && node.Filter == null)
        {
            Add(FindingKind.EmptyCatchBlock, node,
                "Boş catch bloğu. Hata sessizce yutulur.",
                "catch (Exception ex) { _logger.LogError(ex, \"...\"); throw; }");
        }

        base.VisitCatchClause(node);
    }

    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        CheckForStringConcatInLoop(node.Statement);
        base.VisitWhileStatement(node);
    }

    public override void VisitForStatement(ForStatementSyntax node)
    {
        CheckForStringConcatInLoop(node.Statement);
        base.VisitForStatement(node);
    }

    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        CheckForStringConcatInLoop(node.Statement);
        base.VisitForEachStatement(node);
    }

    private void CheckForStringConcatInLoop(StatementSyntax body)
    {
        var assignments = body.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        foreach (var assignment in assignments)
        {
            if (assignment.IsKind(SyntaxKind.AddAssignmentExpression) &&
                assignment.Left is IdentifierNameSyntax id &&
                IsStringVariable(id))
            {
                Add(FindingKind.StringConcatenationInLoop, assignment,
                    $"Döngü içinde string += kullanılıyor. StringBuilder kullan.",
                    "var sb = new StringBuilder(); sb.Append(...);");
            }
        }
    }

    private static bool IsStringVariable(IdentifierNameSyntax id)
    {
        var name = id.Identifier.ValueText;
        return name.Contains("str", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("text", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("result", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHttpClientType(TypeSyntax type) =>
        type.ToString() is "HttpClient" or "System.Net.Http.HttpClient";

    private static bool IsDisposedWithUsing(SyntaxNode node)
    {
        for (var n = node.Parent; n is not null; n = n.Parent)
        {
            if (n is UsingStatementSyntax)
            {
                return true;
            }

            if (n is LocalDeclarationStatementSyntax local && local.UsingKeyword != default)
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeSecret(string value, LiteralExpressionSyntax node)
    {
        if (value.Length < 8)
        {
            return false;
        }

        if (value.StartsWith("sk-", StringComparison.Ordinal) ||
            value.StartsWith("AKIA", StringComparison.Ordinal) ||
            value.Contains("BEGIN PRIVATE KEY", StringComparison.Ordinal))
        {
            return true;
        }

        var assignment = node.Ancestors().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
        var name = assignment?.Parent switch
        {
            VariableDeclaratorSyntax v => v.Identifier.ValueText,
            AssignmentExpressionSyntax a when a.Left is IdentifierNameSyntax id => id.Identifier.ValueText,
            _ => ""
        };

        if (name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Token", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return value.Contains("Password=", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeSqlInjection(string value, LiteralExpressionSyntax node)
    {
        if (!value.Contains("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !value.Contains("INSERT", StringComparison.OrdinalIgnoreCase) &&
            !value.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) &&
            !value.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
            {
                return true;
            }

            if (parent is InterpolatedStringExpressionSyntax)
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }

    private void Add(FindingKind kind, SyntaxNode node, string message, string patch)
    {
        var line = _text.Lines.GetLinePosition(node.SpanStart);
        var snippet = node.GetText().ToString().Trim();
        if (snippet.Length > 160)
        {
            snippet = snippet[..157] + "...";
        }

        _findings.Add(new Finding(kind, _path, line.Line + 1, line.Character + 1, message, snippet, patch.Trim()));
    }
}
