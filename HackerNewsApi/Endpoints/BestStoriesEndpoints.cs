namespace HackerNewsApi.Endpoints;

using HackerNewsApi.Services;

public static class BestStoriesEndpoints
{
    public static IEndpointRouteBuilder MapBestStoriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/beststories");

        group.MapGet("/{n:int}", async (int n, IHackerNewsService hackerNewsService) =>
        {
            if (n <= 0)
            {
                return Results.BadRequest("n must be a positive integer.");
            }

            var stories = await hackerNewsService.GetBestStoriesAsync(n);
            return Results.Ok(stories);
        })
        .WithName("GetBestStories")
        .WithSummary("Returns the first n best stories sorted by score descending.")
        .WithDescription("Retrieves the top n best stories from the Hacker News API, sorted by score in descending order.")
        .WithTags("Best Stories");

        return app;
    }
}
