using LocalInference.Domain.Enums;

namespace LocalInference.Application.Abstractions.Inference;

public interface IInferenceProviderFactory
{
    IInferenceProvider GetProvider(InferenceProviderType providerType);
    IInferenceProvider GetProvider(string modelIdentifier);
    bool SupportsProvider(InferenceProviderType providerType);
    IReadOnlyList<string> GetAvailableModels();
}
