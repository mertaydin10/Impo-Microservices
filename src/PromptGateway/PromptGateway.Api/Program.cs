using System.Text;
using System.Text.Json;
using PromptGateway.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BudgetBook>();
builder.Services.AddSingleton<AuditLog>();
builder.Services.AddSingleton<ModelRouter>();
builder.Services.AddHealthChecks();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "PromptGateway API", 
        Version = "v1",
        Description = "LLM Gateway - PII masking, rate limiting, model routing ve audit logging"
    });
});

var app = builder.Build();
app.Services.GetRequiredService<AuditLog>().SeedDemo();
var budget = app.Services.GetRequiredService<BudgetBook>();
budget.TryConsume("sales-desk", 38, out _);
budget.TryConsume("support", 91, out _);
budget.TryConsume("team-a", 54, out _);
budget.TryConsume("hr-bot", 22, out _);
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PromptGateway API v1");
    c.RoutePrefix = "swagger";
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealthChecks("/health");

app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/v1"), branch =>
{
    branch.Use(async (ctx, next) =>
    {
        ctx.Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }

        var masked = PiiMasker.Mask(body);
        var router = ctx.RequestServices.GetRequiredService<ModelRouter>();
        masked = RewriteModel(masked, router);

        var key = ctx.Request.Headers.Authorization.FirstOrDefault()
                  ?? ctx.Request.Headers["X-Api-Key"].FirstOrDefault()
                  ?? "anon";
        var tokens = Math.Max(1, masked.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        var budget = ctx.RequestServices.GetRequiredService<BudgetBook>();
        if (!budget.TryConsume(key, tokens, out var remaining))
        {
            ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await ctx.Response.WriteAsJsonAsync(new { error = "budget_exceeded", remaining });
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(masked);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        ctx.Request.Headers.ContentLength = bytes.Length;

        var model = TryModel(masked);
        ctx.RequestServices.GetRequiredService<AuditLog>().Add(new AuditRow(
            DateTimeOffset.UtcNow, key, model, tokens, ctx.Request.Path, Truncate(masked, 240)));

        await next();
    });
});

app.MapPost("/_upstream/v1/chat/completions", async (HttpRequest request) =>
{
    using var doc = await JsonDocument.ParseAsync(request.Body);
    var model = doc.RootElement.TryGetProperty("model", out var m) ? m.GetString() : "unknown";
    var prompt = "";
    if (doc.RootElement.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0)
    {
        prompt = messages[0].TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
    }

    return Results.Json(new
    {
        id = "cmpl-mock",
        model,
        choices = new[]
        {
            new { index = 0, message = new { role = "assistant", content = $"[{model}] PII maskelenmiş girdi özeti: {Truncate(prompt, 180)}" } }
        }
    });
});

app.MapGet("/admin/audit", (AuditLog log) => log.Recent())
.WithName("GetAudit")
.WithTags("Admin");

app.MapGet("/admin/budget", (BudgetBook book) => book.Snapshot())
.WithName("GetBudget")
.WithTags("Admin");

app.MapPost("/admin/budget/reset/{key}", (string key, BudgetBook book) =>
{
    book.Reset(key);
    return Results.Ok(new { message = $"Budget reset for {key}" });
})
.WithName("ResetBudget")
.WithTags("Admin");

app.MapGet("/admin/routes", (ModelRouter router) => router.GetRoutes())
.WithName("GetRoutes")
.WithTags("Admin");

app.MapPost("/admin/routes", (RouteRequest req, ModelRouter router) =>
{
    router.AddRoute(req.Alias, req.Target);
    return Results.Ok(new { message = "Route added", alias = req.Alias, target = req.Target });
})
.WithName("AddRoute")
.WithTags("Admin");

app.MapReverseProxy();
app.Run();

static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "…";

static string TryModel(string json)
{
    try
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("model", out var m) ? m.GetString() ?? "" : "";
    }
    catch (JsonException)
    {
        return "";
    }
}

static string RewriteModel(string json, ModelRouter router)
{
    try
    {
        using var doc = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            var picked = router.Pick(doc.RootElement.TryGetProperty("model", out var req) ? req.GetString() : null);
            writer.WriteString("model", picked);
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (p.NameEquals("model"))
                {
                    continue;
                }

                p.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
    catch (JsonException)
    {
        return json;
    }
}

record RouteRequest(string Alias, string Target);
