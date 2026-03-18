using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Application.Abstractions.Retrieval;
using LocalInference.Domain.Entities;
using LocalInference.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LocalInference.Api.Endpoints;

public static class RetrievalEndpoints
{
    public static IEndpointRouteBuilder MapRetrievalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/retrieval").WithTags("Retrieval");

        group.MapPost("/query", async (
            [FromBody] RetrievalQueryRequest request,
            ITechnicalRetrievalService retrievalService,
            CancellationToken cancellationToken) =>
        {
            var options = new RetrievalOptions
            {
                MaxResults = request.MaxResults ?? 5,
                MaxTokens = request.MaxTokens ?? 2000,
                MinScore = request.MinScore ?? 0.7,
                DocumentTypes = request.DocumentTypes,
                Language = request.Language,
                PrioritizeRecent = request.PrioritizeRecent ?? true
            };

            var results = await retrievalService.RetrieveAsync(request.Query, options, cancellationToken);

            return Results.Ok(new RetrievalQueryResponse
            {
                Query = request.Query,
                Results = results.Select(r => new RetrievalResultDto
                {
                    Content = r.Content,
                    Source = r.Source,
                    Score = r.Score,
                    TokenCount = r.TokenCount,
                    DocumentType = r.DocumentType,
                    Language = r.Language,
                    ChunkIndex = r.ChunkIndex
                }).ToList()
            });
        })
        .WithName("QueryRetrieval")
        .WithOpenApi()
        .Produces<RetrievalQueryResponse>(200);

        group.MapGet("/documents", async (
            ITechnicalDocumentRepository documentRepository,
            CancellationToken cancellationToken) =>
        {
            var documents = await documentRepository.GetAllAsync(cancellationToken);
            return Results.Ok(documents.Select(MapToDto));
        })
        .WithName("ListDocuments")
        .WithOpenApi()
        .Produces<IReadOnlyList<TechnicalDocumentDto>>(200);

        group.MapPost("/documents", async (
            [FromBody] CreateDocumentRequest request,
            ITechnicalDocumentRepository documentRepository,
            CancellationToken cancellationToken) =>
        {
            var document = TechnicalDocument.Create(
                request.Title,
                request.Content,
                Enum.Parse<DocumentType>(request.DocumentType),
                request.SourceUrl,
                request.SourcePath,
                request.Language,
                request.Framework,
                request.Version);

            await documentRepository.AddAsync(document, cancellationToken);

            return Results.Created($"/api/retrieval/documents/{document.Id}", MapToDto(document));
        })
        .WithName("CreateDocument")
        .WithOpenApi()
        .Produces<TechnicalDocumentDto>(201);

        group.MapPost("/documents/{id:guid}/index", async (
            Guid id,
            ITechnicalRetrievalService retrievalService,
            CancellationToken cancellationToken) =>
        {
            await retrievalService.IndexDocumentAsync(id, cancellationToken);
            return Results.NoContent();
        })
        .WithName("IndexDocument")
        .WithOpenApi()
        .Produces(204);

        group.MapPost("/reindex", async (
            ITechnicalRetrievalService retrievalService,
            CancellationToken cancellationToken) =>
        {
            await retrievalService.ReindexAllAsync(cancellationToken);
            return Results.NoContent();
        })
        .WithName("ReindexAll")
        .WithOpenApi()
        .Produces(204);

        return app;
    }

    private static TechnicalDocumentDto MapToDto(TechnicalDocument document)
    {
        return new TechnicalDocumentDto
        {
            Id = document.Id,
            Title = document.Title,
            Content = document.Content.Length > 500 ? document.Content[..500] + "..." : document.Content,
            DocumentType = document.DocumentType.ToString(),
            SourceUrl = document.SourceUrl,
            SourcePath = document.SourcePath,
            Language = document.Language,
            Framework = document.Framework,
            Version = document.Version,
            TokenCount = document.TokenCount,
            IsIndexed = document.IsIndexed,
            LastIndexedAt = document.LastIndexedAt,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }
}

public sealed class RetrievalQueryRequest
{
    public required string Query { get; set; }
    public int? MaxResults { get; set; }
    public int? MaxTokens { get; set; }
    public double? MinScore { get; set; }
    public List<string>? DocumentTypes { get; set; }
    public string? Language { get; set; }
    public bool? PrioritizeRecent { get; set; }
}

public sealed class RetrievalQueryResponse
{
    public required string Query { get; set; }
    public required List<RetrievalResultDto> Results { get; set; }
}

public sealed class RetrievalResultDto
{
    public required string Content { get; set; }
    public required string Source { get; set; }
    public required double Score { get; set; }
    public required int TokenCount { get; set; }
    public string? DocumentType { get; set; }
    public string? Language { get; set; }
    public int? ChunkIndex { get; set; }
}

public sealed class TechnicalDocumentDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string DocumentType { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourcePath { get; set; }
    public string? Language { get; set; }
    public string? Framework { get; set; }
    public string? Version { get; set; }
    public int TokenCount { get; set; }
    public bool IsIndexed { get; set; }
    public DateTime? LastIndexedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateDocumentRequest
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string DocumentType { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourcePath { get; set; }
    public string? Language { get; set; }
    public string? Framework { get; set; }
    public string? Version { get; set; }
}
