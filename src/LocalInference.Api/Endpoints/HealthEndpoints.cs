namespace LocalInference.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
        app.MapGet("/v1/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        return app;
    }
}
