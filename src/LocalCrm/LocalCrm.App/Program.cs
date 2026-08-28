using LocalCrm.App;
using LocalCrm.App.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var dir = Path.Combine(env.ContentRootPath, "App_Data");
    Directory.CreateDirectory(dir);
    return new LocalStore(Path.Combine(dir, "local.db"));
});

builder.Services.AddHttpClient<SyncClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CloudUrl"] ?? "http://localhost:5057");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
