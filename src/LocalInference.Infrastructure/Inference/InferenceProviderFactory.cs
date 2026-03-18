using LocalInference.Application.Abstractions.Inference;
using LocalInference.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace LocalInference.Infrastructure.Inference;

public class InferenceProviderFactory : IInferenceProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<InferenceProviderType, Type> _providerTypes;

    public InferenceProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _providerTypes = new Dictionary<InferenceProviderType, Type>
        {
            [InferenceProviderType.Ollama] = typeof(OllamaInferenceProvider),
            [InferenceProviderType.OpenRouter] = typeof(OpenRouterInferenceProvider)
        };
    }

    public IInferenceProvider GetProvider(InferenceProviderType providerType)
    {
        if (!_providerTypes.TryGetValue(providerType, out var providerTypeImpl))
        {
            throw new NotSupportedException($"Provider type '{providerType}' is not supported");
        }

        return (IInferenceProvider)_serviceProvider.GetRequiredService(providerTypeImpl);
    }

    public IInferenceProvider GetProvider(string modelIdentifier)
    {
        if (modelIdentifier.Contains('/'))
        {
            return GetProvider(InferenceProviderType.OpenRouter);
        }

        return GetProvider(InferenceProviderType.Ollama);
    }

    public bool SupportsProvider(InferenceProviderType providerType)
    {
        return _providerTypes.ContainsKey(providerType);
    }

    public IReadOnlyList<string> GetAvailableModels()
    {
        return new List<string>();
    }
}
