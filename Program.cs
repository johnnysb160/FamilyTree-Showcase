using System.Globalization;
using FamilyTree.Components;
using FamilyTree.Data;
using FamilyTree.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("FamilyTree")));
builder.Services.AddScoped<FamilyTreeService>();
builder.Services.AddSingleton<PhotoStorage>();

var app = builder.Build();

// Create/upgrade the SQLite database and apply the initial load (HasData)
using (var scope = app.Services.CreateScope())
{
    await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Photos uploaded through the admin
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(app.Services.GetRequiredService<PhotoStorage>().Folder),
    RequestPath = PhotoStorage.RequestPath
});

// Flat and hierarchical JSON used by the visualizations
var api = app.MapGroup("/api");
api.MapGet("/family", async (FamilyTreeService s) => FamilyTreeService.Family(await s.ListAsync()));
api.MapGet("/tree/{id:int}/ancestors", async (int id, FamilyTreeService s) => FamilyTreeService.Ancestors(await s.ListAsync(), id));
api.MapGet("/tree/{id:int}/descendants", async (int id, FamilyTreeService s) => FamilyTreeService.Descendants(await s.ListAsync(), id));
api.MapGet("/tree/{id:int}/bidirectional", async (int id, FamilyTreeService s) => FamilyTreeService.Bidirectional(await s.ListAsync(), id));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
