using LocalInference.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocalInference.Api.Endpoints;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions").WithTags("Sessions");

        group.MapPost("/", async (
            [FromBody] CreateSessionApiRequest request,
            ISessionManagementService sessionService,
            CancellationToken cancellationToken) =>
        {
            var session = await sessionService.CreateSessionAsync(new CreateSessionRequest
            {
                Name = request.Name,
                Description = request.Description,
                InferenceConfigId = request.InferenceConfigId,
                ContextWindowTokens = request.ContextWindowTokens,
                MaxOutputTokens = request.MaxOutputTokens
            }, cancellationToken);

            return Results.Created($"/api/sessions/{session.Id}", session);
        })
        .WithName("CreateSession")
        .WithOpenApi()
        .Produces<SessionDto>(201);

        group.MapGet("/", async (
            [FromQuery] bool? activeOnly,
            [FromQuery] int? skip,
            [FromQuery] int? take,
            ISessionManagementService sessionService,
            CancellationToken cancellationToken) =>
        {
            var sessions = await sessionService.ListSessionsAsync(new ListSessionsRequest
            {
                ActiveOnly = activeOnly ?? false,
                Skip = skip ?? 0,
                Take = take ?? 100
            }, cancellationToken);

            return Results.Ok(sessions);
        })
        .WithName("ListSessions")
        .WithOpenApi()
        .Produces<IReadOnlyList<SessionSummaryDto>>(200);

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISessionManagementService sessionService,
            CancellationToken cancellationToken) =>
        {
            var session = await sessionService.GetSessionAsync(id, cancellationToken);
            return session == null ? Results.NotFound() : Results.Ok(session);
        })
        .WithName("GetSession")
        .WithOpenApi()
        .Produces<SessionDto>(200)
        .Produces(404);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSessionApiRequest request,
            ISessionManagementService sessionService,
            CancellationToken cancellationToken) =>
        {
            await sessionService.UpdateSessionAsync(id, new UpdateSessionRequest
            {
                Name = request.Name,
                Description = request.Description,
                InferenceConfigId = request.InferenceConfigId,
                ContextWindowTokens = request.ContextWindowTokens
            }, cancellationToken);

            return Results.NoContent();
        })
        .WithName("UpdateSession")
        .WithOpenApi()
        .Produces(204)
        .Produces(404);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISessionManagementService sessionService,
            CancellationToken cancellationToken) =>
        {
            await sessionService.DeleteSessionAsync(id, cancellationToken);
            return Results.NoContent();
        })
        .WithName("DeleteSession")
        .WithOpenApi()
        .Produces(204);

        group.MapGet("/{id:guid}/statistics", async (
            Guid id,
            ISessionManagementService sessionService,
            CancellationToken cancellationToken) =>
        {
            var stats = await sessionService.GetSessionStatisticsAsync(id, cancellationToken);
            return Results.Ok(stats);
        })
        .WithName("GetSessionStatistics")
        .WithOpenApi()
        .Produces<SessionStatistics>(200);

        group.MapPost("/{id:guid}/clear", async (
            Guid id,
            ISessionManagementService sessionService,
            CancellationToken cancellationToken) =>
        {
            await sessionService.ClearSessionContextAsync(id, cancellationToken);
            return Results.NoContent();
        })
        .WithName("ClearSessionContext")
        .WithOpenApi()
        .Produces(204);

        return app;
    }
}

public sealed class CreateSessionApiRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? InferenceConfigId { get; set; }
    public int? ContextWindowTokens { get; set; }
    public int? MaxOutputTokens { get; set; }
}

public sealed class UpdateSessionApiRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? InferenceConfigId { get; set; }
    public int? ContextWindowTokens { get; set; }
}
