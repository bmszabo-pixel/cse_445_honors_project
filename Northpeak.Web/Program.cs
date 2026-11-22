using Microsoft.FluentUI.AspNetCore.Components;
using Northpeak.Web.Components;
using Northpeak.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var backendUrl = builder.Configuration["BackendApi:BaseUrl"]
    ?? throw new InvalidOperationException("BackendApi:BaseUrl is missing from appsettings.json.");

builder.Services.AddHttpClient<ChatService>(client =>
{
    client.BaseAddress = new Uri(backendUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(90);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseRequestLocalization("en-US");
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
