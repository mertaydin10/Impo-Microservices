using LocalCrm.Api;
using LocalCrm.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(new CloudStore(Path.Combine(builder.Environment.ContentRootPath, "cloud.db")));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "LocalCrm API", 
        Version = "v1",
        Description = "Local-first CRM - Field-level merging ile offline çalışma ve bulut senkronizasyonu"
    });
});

var app = builder.Build();
app.Services.GetRequiredService<CloudStore>().SeedIfEmpty();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LocalCrm API v1");
    c.RoutePrefix = "swagger";
});

app.MapHealthChecks("/health");

app.MapGet("/contacts", (CloudStore store) => store.All())
.WithName("GetAllContacts")
.WithTags("Contacts");

app.MapGet("/contacts/{id:guid}", (Guid id, CloudStore store) =>
{
    var contact = store.Get(id);
    return contact is null ? Results.NotFound() : Results.Ok(contact);
})
.WithName("GetContact")
.WithTags("Contacts");

app.MapPost("/sync", (SyncBatch batch, CloudStore store) =>
{
    var results = new List<object>();
    var errors = new List<string>();
    
    foreach (var contact in batch.Contacts)
    {
        var validation = contact.Validate();
        if (!validation.IsValid)
        {
            errors.AddRange(validation.Errors.Select(e => $"Contact {contact.Id}: {e}"));
            continue;
        }
        
        try
        {
            var merged = store.Upsert(contact);
            results.Add(new { contact = merged, status = "merged" });
        }
        catch (Exception ex)
        {
            errors.Add($"Contact {contact.Id}: {ex.Message}");
        }
    }
    
    if (errors.Count > 0 && results.Count == 0)
    {
        return Results.BadRequest(new { errors });
    }
    
    return Results.Ok(new { contacts = results.Select(r => ((dynamic)r).contact).ToList(), errors });
})
.WithName("SyncContacts")
.WithTags("Contacts");

app.MapDelete("/contacts/{id:guid}", (Guid id, CloudStore store) =>
{
    var deleted = store.Delete(id);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteContact")
.WithTags("Contacts");

app.MapGet("/stats", (CloudStore store) => Results.Ok(store.GetStats()))
.WithName("GetStats")
.WithTags("Monitoring");

app.Run();

public sealed record SyncBatch(List<ContactRecord> Contacts);
