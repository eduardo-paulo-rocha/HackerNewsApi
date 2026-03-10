namespace HackerNewsApi.Services;

using HackerNewsApi.Models;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Service that retrieves stories from the Hacker News API with in-memory caching
/// to avoid overloading the upstream API under heavy request volumes.
/// </summary>
public interface IHackerNewsService
{
    /// <summary>
    /// Returns the first <paramref name="count"/> best stories sorted by score descending.
    /// </summary>
    Task<IReadOnlyList<StoryResponse>> GetBestStoriesAsync(int count);
}

public class HackerNewsService : IHackerNewsService
{
    private const string BestStoriesUrl = "https://hacker-news.firebaseio.com/v0/beststories.json";
    private const string ItemUrl = "https://hacker-news.firebaseio.com/v0/item/{0}.json";
    private const string BestStoriesCacheKey = "BestStoryIds";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HackerNewsService> _logger;
    private readonly TimeSpan _bestStoriesCacheDuration = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _storyCacheDuration = TimeSpan.FromMinutes(5);

    // Semaphore to limit concurrent calls to the HN API, preventing overload.
    private static readonly SemaphoreSlim _throttle = new(20);

    public HackerNewsService(HttpClient httpClient, IMemoryCache cache, ILogger<HackerNewsService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StoryResponse>> GetBestStoriesAsync(int count)
    {
        var storyIds = await GetBestStoryIdsAsync();

        // Fetch story details in parallel (with throttling) for the requested count.
        var tasks = storyIds
            .Take(count)
            .Select(GetStoryAsync);

        var stories = await Task.WhenAll(tasks);

        return stories
            .Where(s => s is not null)
            .OrderByDescending(s => s!.Score)
            .ToList()!;
    }

    private async Task<List<int>> GetBestStoryIdsAsync()
    {
        if (_cache.TryGetValue(BestStoriesCacheKey, out List<int>? cachedIds) && cachedIds is not null)
        {
            return cachedIds;
        }

        _logger.LogInformation("Fetching best story IDs from Hacker News API");
        var ids = await _httpClient.GetFromJsonAsync<List<int>>(BestStoriesUrl)
                  ?? new List<int>();

        _cache.Set(BestStoriesCacheKey, ids, _bestStoriesCacheDuration);
        return ids;
    }

    private async Task<StoryResponse?> GetStoryAsync(int storyId)
    {
        var cacheKey = $"story_{storyId}";

        if (_cache.TryGetValue(cacheKey, out StoryResponse? cached) && cached is not null)
        {
            return cached;
        }

        await _throttle.WaitAsync();
        try
        {
            // Double-check cache after acquiring semaphore (another thread may have populated it).
            if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var url = string.Format(ItemUrl, storyId);
            var item = await _httpClient.GetFromJsonAsync<HackerNewsItem>(url);

            if (item is null)
            {
                return null;
            }

            var response = MapToResponse(item);
            _cache.Set(cacheKey, response, _storyCacheDuration);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch story {StoryId}", storyId);
            return null;
        }
        finally
        {
            _throttle.Release();
        }
    }

    private static StoryResponse MapToResponse(HackerNewsItem item)
    {
        return new StoryResponse
        {
            Title = item.Title,
            Uri = item.Url,
            PostedBy = item.By,
            Time = DateTimeOffset.FromUnixTimeSeconds(item.Time).ToString("yyyy-MM-ddTHH:mm:sszzz"),
            Score = item.Score,
            CommentCount = item.Descendants
        };
    }
}
