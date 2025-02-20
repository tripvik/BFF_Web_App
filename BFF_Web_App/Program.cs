using BFF_Web_App;
using BFF_Web_App.Client.Weather;
using BFF_Web_App.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FluentUI.AspNetCore.Components;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEntraAuthentication(builder.Configuration);

//builder.Services.AddOIDCAuthentication(builder.Configuration);

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddFluentUIComponents();


builder.Services.AddHttpForwarderWithServiceDiscovery();
builder.Services.AddHttpContextAccessor();

var apiBaseAddress = "http://localhost:5041";

//For ACA
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
{
    apiBaseAddress = "http://weatherapi";
}

builder.Services.AddHttpClient<IWeatherForecaster, ServerWeatherForecaster>(httpClient =>
{
    httpClient.BaseAddress = new(apiBaseAddress);
});

builder.Services.AddScoped<GraphService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    //To make https work
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BFF_Web_App.Client._Imports).Assembly);

if (app.Environment.IsDevelopment())
{
    app.MapForwarder("/weather-forecast", "https://localhost:5041", transformBuilder =>
    {
        transformBuilder.AddRequestTransform(async transformContext =>
        {
            var accessToken = await transformContext.HttpContext.GetTokenAsync("access_token");
            transformContext.ProxyRequest.Headers.Authorization = new("Bearer", accessToken);
        });
    }).RequireAuthorization();
}
else
{
    app.MapForwarder("/weather-forecast", "http://weatherapi", transformBuilder =>
    {
        transformBuilder.AddRequestTransform(async transformContext =>
        {
            var accessToken = await transformContext.HttpContext.GetTokenAsync("access_token");
            transformContext.ProxyRequest.Headers.Authorization = new("Bearer", accessToken);
        });
    }).RequireAuthorization();
}

app.MapGroup("/authentication").MapLoginAndLogout();

app.Run();
