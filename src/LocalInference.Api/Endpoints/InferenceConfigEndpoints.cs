using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Domain.Entities;
using LocalInference.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LocalInference.Api.Endpoints;

public static class InferenceConfigEndpoints
{
    public static IEndpointRouteBuilder MapInferenceConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/configs").WithTags("Inference Configs");

        group.MapGet("/", async (
            IInferenceConfigRepository configRepository,
            CancellationToken cancellationToken) =>
        {
            var configs = await configRepository.GetAllAsync(cancellationToken);
            return Results.Ok(configs.Select(MapToDto));
        })
        .WithName("ListInferenceConfigs")
        .WithOpenApi()
        .Produces<IReadOnlyList<InferenceConfigDto>>(200);

        group.MapGet("/{id:guid}", async (
            Guid id,
            IInferenceConfigRepository configRepository,
            CancellationToken cancellationToken) =>
        {
            var config = await configRepository.GetByIdAsync(id, cancellationToken);
            return config == null ? Results.NotFound() : Results.Ok(MapToDto(config));
        })
        .WithName("GetInferenceConfig")
        .WithOpenApi()
        .Produces<InferenceConfigDto>(200)
        .Produces(404);

        group.MapPost("/", async (
            [FromBody] CreateInferenceConfigRequest request,
            IInferenceConfigRepository configRepository,
            CancellationToken cancellationToken) =>
        {
            var config = InferenceConfig.Create(
                request.Name,
                request.ModelIdentifier,
                Enum.Parse<InferenceProviderType>(request.ProviderType),
                request.Temperature,
                request.TopP,
                request.SystemPrompt);

            config.UpdateParameters(
                maxTokens: request.MaxTokens,
                contextWindow: request.ContextWindow,
                stopSequences: request.StopSequences,
                seed: request.Seed,
                frequencyPenalty: request.FrequencyPenalty,
                presencePenalty: request.PresencePenalty);

            if (request.IsDefault)
            {
                config.SetAsDefault(true);
            }

            await configRepository.AddAsync(config, cancellationToken);

            return Results.Created($"/api/configs/{config.Id}", MapToDto(config));
        })
        .WithName("CreateInferenceConfig")
        .WithOpenApi()
        .Produces<InferenceConfigDto>(201);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateInferenceConfigRequest request,
            IInferenceConfigRepository configRepository,
            CancellationToken cancellationToken) =>
        {
            var config = await configRepository.GetByIdAsync(id, cancellationToken);
            if (config == null) return Results.NotFound();

            if (request.Name != null) config.UpdateName(request.Name);
            if (request.ModelIdentifier != null) config.UpdateModel(request.ModelIdentifier, config.ProviderType);

            config.UpdateParameters(
                temperature: request.Temperature,
                topP: request.TopP,
                maxTokens: request.MaxTokens,
                stopSequences: request.StopSequences,
                systemPrompt: request.SystemPrompt,
                seed: request.Seed,
                frequencyPenalty: request.FrequencyPenalty,
                presencePenalty: request.PresencePenalty);

            if (request.IsDefault.HasValue)
            {
                config.SetAsDefault(request.IsDefault.Value);
            }

            await configRepository.UpdateAsync(config, cancellationToken);
            return Results.NoContent();
        })
        .WithName("UpdateInferenceConfig")
        .WithOpenApi()
        .Produces(204)
        .Produces(404);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IInferenceConfigRepository configRepository,
            CancellationToken cancellationToken) =>
        {
            await configRepository.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        })
        .WithName("DeleteInferenceConfig")
        .WithOpenApi()
        .Produces(204);

        return app;
    }

    private static InferenceConfigDto MapToDto(InferenceConfig config)
    {
        return new InferenceConfigDto
        {
            Id = config.Id,
            Name = config.Name,
            ModelIdentifier = config.ModelIdentifier,
            ProviderType = config.ProviderType.ToString(),
            Temperature = config.Temperature,
            TopP = config.TopP,
            TopK = config.TopK,
            RepeatPenalty = config.RepeatPenalty,
            MaxTokens = config.MaxTokens,
            ContextWindow = config.ContextWindow,
            StopSequences = config.StopSequences,
            SystemPrompt = config.SystemPrompt,
            Seed = config.Seed,
            FrequencyPenalty = config.FrequencyPenalty,
            PresencePenalty = config.PresencePenalty,
            IsDefault = config.IsDefault,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }
}

public sealed class InferenceConfigDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ModelIdentifier { get; set; }
    public required string ProviderType { get; set; }
    public double Temperature { get; set; }
    public double TopP { get; set; }
    public double? TopK { get; set; }
    public double? RepeatPenalty { get; set; }
    public int? MaxTokens { get; set; }
    public int? ContextWindow { get; set; }
    public string? StopSequences { get; set; }
    public string? SystemPrompt { get; set; }
    public int? Seed { get; set; }
    public double? FrequencyPenalty { get; set; }
    public double? PresencePenalty { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateInferenceConfigRequest
{
    public required string Name { get; set; }
    public required string ModelIdentifier { get; set; }
    public required string ProviderType { get; set; }
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.9;
    public int? MaxTokens { get; set; }
    public int? ContextWindow { get; set; }
    public string? StopSequences { get; set; }
    public string? SystemPrompt { get; set; }
    public int? Seed { get; set; }
    public double? FrequencyPenalty { get; set; }
    public double? PresencePenalty { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class UpdateInferenceConfigRequest
{
    public string? Name { get; set; }
    public string? ModelIdentifier { get; set; }
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int? MaxTokens { get; set; }
    public int? ContextWindow { get; set; }
    public string? StopSequences { get; set; }
    public string? SystemPrompt { get; set; }
    public int? Seed { get; set; }
    public double? FrequencyPenalty { get; set; }
    public double? PresencePenalty { get; set; }
    public bool? IsDefault { get; set; }
}
