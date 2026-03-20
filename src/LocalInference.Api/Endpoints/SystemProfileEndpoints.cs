using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LocalInference.Api.Endpoints;

public static class SystemProfileEndpoints
{
    public static IEndpointRouteBuilder MapSystemProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/system-profiles").WithTags("System Profiles");

        group.MapGet("/", async (
            IRepository<SystemProfile> repository,
            CancellationToken cancellationToken) =>
        {
            var profiles = await repository.GetAllAsync(cancellationToken);
            return Results.Ok(profiles);
        })
        .WithName("GetAllSystemProfiles")
        .WithOpenApi()
        .Produces<List<SystemProfile>>(200);

        group.MapGet("/{id}", async (
            Guid id,
            IRepository<SystemProfile> repository,
            CancellationToken cancellationToken) =>
        {
            var profile = await repository.GetByIdAsync(id, cancellationToken);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        })
        .WithName("GetSystemProfileById")
        .WithOpenApi()
        .Produces<SystemProfile>(200)
        .Produces(404);

        group.MapPost("/", async (
            SystemProfile profile,
            IRepository<SystemProfile> repository,
            CancellationToken cancellationToken) =>
        {
            var createdProfile = await repository.AddAsync(profile, cancellationToken);
            return Results.Created($"/api/system-profiles/{createdProfile.Id}", createdProfile);
        })
        .WithName("CreateSystemProfile")
        .WithOpenApi()
        .Produces<SystemProfile>(201);

        group.MapPut("/{id}", async (
            Guid id,
            SystemProfile profile,
            IRepository<SystemProfile> repository,
            CancellationToken cancellationToken) =>
        {
            if (id != profile.Id)
                return Results.BadRequest();

            var updated = await repository.UpdateAsync(profile, cancellationToken);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateSystemProfile")
        .WithOpenApi()
        .Produces(204)
        .Produces(400)
        .Produces(404);

        group.MapDelete("/{id}", async (
            Guid id,
            IRepository<SystemProfile> repository,
            CancellationToken cancellationToken) =>
        {
            var deleted = await repository.DeleteAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSystemProfile")
        .WithOpenApi()
        .Produces(204)
        .Produces(404);

        return app;
    }
}

// Repository interface for generic CRUD operations
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}