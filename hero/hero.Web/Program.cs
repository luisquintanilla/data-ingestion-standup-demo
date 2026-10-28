using hero.Web.Components;
using hero.Web.Services;
using hero.Web.Services.Ingestion;
using Standup;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
var app = builder.Build();
app.MapDefaultEndpoints();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseAntiforgery();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapGet("/demo/status", () => new
{
    Snapshot = "S0 (unofficial prototype)", Provider = DemoConfiguration.Provider,
    Index = DemoConfiguration.IndexName(DemoConfiguration.Provider),
    LiveEnabled = Environment.GetEnvironmentVariable("STANDUP_LIVE") == "1",
    SensitiveTelemetry = false
});
app.MapGet("/demo/native-evidence", async (DataIngestor ingestor, CancellationToken ct) =>
{
    if (DemoConfiguration.Provider != "pdfpig")
        return Results.Conflict(new { Error = "CU analysis requires an explicit approved live action; no automatic analysis or native fallback." });
    return Results.Ok(await ingestor.InspectAsync(ct));
});
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
