var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", jwtOptions =>
    {
        // The API does not require an app registration of its own, but it does require a registration for the calling app.
        // These attributes can be found in the Entra ID portal when registering the client.
        jwtOptions.Authority = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/";
        jwtOptions.Audience  = "api://541344a2-eb7a-44b6-a65c-c8352fb81bf8";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weather-forecast", (HttpRequest request) =>
{
    Console.WriteLine("You got hit");
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
}).RequireAuthorization();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
