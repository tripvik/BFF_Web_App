using Microsoft.Graph;
using Microsoft.Identity.Web;

// Some code omitted for brevity.

[AuthorizeForScopes(Scopes = new[] { "User.Read" })]
public class GraphService
{
    private readonly ILogger<GraphService> _logger;
    private readonly GraphServiceClient _graphServiceClient;

    public GraphService(ILogger<GraphService> logger, GraphServiceClient graphServiceClient)
    {
        _logger = logger;
        _graphServiceClient = graphServiceClient;
    }

    public async Task<string> GetProfileImageAsync()
    {
        try
        {
            var user = await _graphServiceClient.Me.GetAsync();

            using (var photoStream = await _graphServiceClient.Me.Photo.Content.GetAsync())
            {
                using (var memoryStream = new MemoryStream())
                {
                    await photoStream.CopyToAsync(memoryStream);
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            return "Not Found";
        }
    }
}