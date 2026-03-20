using LocalInference.Application.Abstractions.Inference;
using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Application.Abstractions.Retrieval;
using LocalInference.Application.Abstractions.Summarization;
using LocalInference.Application.Services;
using LocalInference.Infrastructure.Inference;
using LocalInference.Infrastructure.Persistence;
using LocalInference.Infrastructure.Persistence.Repositories;
using LocalInference.Infrastructure.Retrieval;
using LocalInference.Infrastructure.Summarization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocalInference.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=LocalInference;Username=postgres;Password=postgres";

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IInferenceConfigRepository, InferenceConfigRepository>();
        services.AddScoped<ITechnicalDocumentRepository, TechnicalDocumentRepository>();

        services.AddHttpClient<OllamaInferenceProvider>(client =>
        {
            var baseUrl = configuration["Inference:Ollama:BaseUrl"] ?? "http://localhost:11434";
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(300);
        });

        services.AddHttpClient<OpenRouterInferenceProvider>(client =>
        {
            var apiKey = configuration["Inference:OpenRouter:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
            client.BaseAddress = new Uri("https://openrouter.ai/");
            client.Timeout = TimeSpan.FromSeconds(300);
        });

        services.AddHttpClient<OllamaEmbeddingProvider>(client =>
        {
            var baseUrl = configuration["Inference:Ollama:BaseUrl"] ?? "http://localhost:11434";
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddScoped<IInferenceProviderFactory, InferenceProviderFactory>();
        services.AddScoped<IEmbeddingProvider, OllamaEmbeddingProvider>();
        services.AddScoped<ITechnicalRetrievalService, TechnicalRetrievalService>();
        services.AddScoped<ITechnicalSummarizationService, TechnicalSummarizationService>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<LocalInference.Application.Abstractions.Inference.IInferenceService, LocalInference.Application.Services.InferenceService>();
        services.AddScoped<ISessionManagementService, SessionManagementService>();
        services.AddScoped<IContextManager, ContextManager>();

        return services;
    }
}
