using Microsoft.AspNetCore.Authentication;
using BFF_Web_App.Client.Weather;
using Microsoft.Identity.Web;
using Microsoft.Graph.Models.ExternalConnectors;
using Microsoft.Extensions.Configuration;

namespace BFF_Web_App;

internal sealed class ServerWeatherForecaster : IWeatherForecaster
{
    private readonly ITokenAcquisition _tokenAquisitionService;
    private readonly HttpClient httpClient;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IConfiguration _configuration;

    public ServerWeatherForecaster(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ITokenAcquisition tokenAquisitionService, IConfiguration configuration)
    {
        this.httpClient = httpClient;
        this.httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _tokenAquisitionService = tokenAquisitionService;
    }

    public async Task<IEnumerable<WeatherForecast>> GetWeatherForecastAsync()
    {
        string[] scopes = _configuration.GetSection("DownstreamApis:WeatherForecast:Scopes").Get<string[]>();

        var accessToken = await _tokenAquisitionService.GetAccessTokenForUserAsync(scopes) ??
            throw new InvalidOperationException("No access_token was received");

        var httpContext = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No HttpContext available from the IHttpContextAccessor!");

        //var accessToken = await httpContext.GetTokenAsync("access_token") ??
        //    throw new InvalidOperationException("No access_token was saved");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/weather-forecast");
        requestMessage.Headers.Authorization = new("Bearer", accessToken);
        using var response = await httpClient.SendAsync(requestMessage);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<WeatherForecast[]>() ??
            throw new IOException("No weather forecast!");
    }
}
