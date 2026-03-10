namespace HackerNewsApi.Controllers;

using HackerNewsApi.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BestStoriesController : ControllerBase
{
    private readonly IHackerNewsService _hackerNewsService;

    public BestStoriesController(IHackerNewsService hackerNewsService)
    {
        _hackerNewsService = hackerNewsService;
    }

    /// <summary>
    /// Returns the first <paramref name="n"/> best stories sorted by score descending.
    /// </summary>
    /// <param name="n">The number of best stories to return.</param>
    [HttpGet("{n:int}")]
    public async Task<IActionResult> GetBestStories(int n)
    {
        if (n <= 0)
        {
            return BadRequest("n must be a positive integer.");
        }

        var stories = await _hackerNewsService.GetBestStoriesAsync(n);
        return Ok(stories);
    }
}
