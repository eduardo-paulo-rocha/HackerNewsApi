using HackerNewsApi.Endpoints;
using HackerNewsApi.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register in-memory caching for story IDs and story details.
builder.Services.AddMemoryCache();

// Register a typed HttpClient for the Hacker News upstream API.
builder.Services.AddHttpClient<IHackerNewsService, HackerNewsService>(client =>
{
    client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register the built-in ASP.NET Core OpenAPI document generator.
// This produces the OpenAPI specification at /openapi/v1.json at runtime.
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();

// Expose OpenAPI and Scalar only in the Development environment
// to avoid leaking API internals in production.
if (app.Environment.IsDevelopment())
{
    // Serves the OpenAPI JSON document at /openapi/v1.json.
    app.MapOpenApi();

    // Serves the Scalar interactive documentation UI at /scalar.
    // The UI is configured to load the OpenAPI document generated above.
    app.MapScalarApiReference(options =>
    {
        options.Title = "Hacker News Best Stories API";
        options.OpenApiRoutePattern = "/openapi/v1.json";
    });
}

// Register the minimal API endpoints.
app.MapBestStoriesEndpoints();

app.Run();
