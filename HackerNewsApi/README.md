# Hacker News Best Stories API

A RESTful API built with ASP.NET Core that retrieves the details of the top _n_ "best stories" from the [Hacker News API](https://github.com/HackerNews/API), sorted by score in descending order.

## How to Run

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)

### Running the Application

```bash
cd HackerNewsApi
dotnet run
```

The API will start on `https://localhost:5001` (or the port shown in console output).

### Usage

Retrieve the top _n_ best stories:

```
GET /api/beststories/{n}
```

**Example:**

```bash
curl https://localhost:5001/api/beststories/10
```

**Response:**

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1757,
    "commentCount": 588
  }
]
```

## Assumptions

- **"First n best stories"** refers to the first _n_ IDs returned by the `/v0/beststories` endpoint, which are then sorted by score descending before being returned to the caller.
- The Hacker News API is publicly available and does not require authentication.
- Stories that fail to load (e.g. deleted or network errors) are silently skipped rather than causing the entire request to fail.

## Design Decisions

### Caching

- **In-memory caching** (`IMemoryCache`) is used both for the best story ID list and individual story details, each with a 5-minute TTL.
- This ensures that repeated requests within the cache window are served instantly without hitting the Hacker News API.

### Concurrency Throttling

- A `SemaphoreSlim(20)` limits concurrent outbound HTTP requests to the Hacker News API, preventing overload while still fetching stories efficiently in parallel.
- A double-check pattern is used after acquiring the semaphore to avoid redundant API calls.

### Typed HttpClient

- The `HackerNewsService` is registered via `AddHttpClient<>`, giving it a managed `HttpClient` with proper lifecycle handling via `IHttpClientFactory`.

## Enhancements (Given More Time)

- **Distributed caching** (e.g. Redis) to share cache across multiple instances in a scaled-out deployment.
- **Response caching / HTTP cache headers** (`Cache-Control`, `ETag`) so downstream clients and CDNs can cache responses.
- **Rate limiting middleware** (e.g. `Microsoft.AspNetCore.RateLimiting`) to protect the API from abuse.
- **Health check endpoint** for monitoring in production.
- **OpenAPI/Swagger documentation** for interactive API exploration.
- **Unit and integration tests** with mocked `HttpClient` to verify caching, error handling, and response mapping.
- **Configuration via `appsettings.json`** for cache durations, concurrency limits, and HN API base URL instead of hard-coded values.
- **Pagination support** (e.g. `?page=1&pageSize=20`) for large result sets.
