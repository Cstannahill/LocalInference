using LocalInference.Domain.Common;
using LocalInference.Domain.Enums;

namespace LocalInference.Domain.Entities;

public sealed class InferenceConfig : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string ModelIdentifier { get; private set; } = string.Empty;
    public InferenceProviderType ProviderType { get; private set; }
    public double Temperature { get; private set; }
    public double TopP { get; private set; }
    public double? TopK { get; private set; }
    public double? RepeatPenalty { get; private set; }
    public int? MaxTokens { get; private set; }
    public int? ContextWindow { get; private set; }
    public string? StopSequences { get; private set; }
    public string? SystemPrompt { get; private set; }
    public int? Seed { get; private set; }
    public double? FrequencyPenalty { get; private set; }
    public double? PresencePenalty { get; private set; }
    public bool IsDefault { get; private set; }

    private InferenceConfig() { }

    public static InferenceConfig Create(
        string name,
        string modelIdentifier,
        InferenceProviderType providerType,
        double temperature = 0.7,
        double topP = 0.9,
        string? systemPrompt = null)
    {
        return new InferenceConfig
        {
            Name = name,
            ModelIdentifier = modelIdentifier,
            ProviderType = providerType,
            Temperature = temperature,
            TopP = topP,
            SystemPrompt = systemPrompt
        };
    }

    public void UpdateParameters(
        double? temperature = null,
        double? topP = null,
        double? topK = null,
        double? repeatPenalty = null,
        int? maxTokens = null,
        int? contextWindow = null,
        string? stopSequences = null,
        string? systemPrompt = null,
        int? seed = null,
        double? frequencyPenalty = null,
        double? presencePenalty = null)
    {
        if (temperature.HasValue) Temperature = temperature.Value;
        if (topP.HasValue) TopP = topP.Value;
        if (topK.HasValue) TopK = topK.Value;
        if (repeatPenalty.HasValue) RepeatPenalty = repeatPenalty.Value;
        if (maxTokens.HasValue) MaxTokens = maxTokens.Value;
        if (contextWindow.HasValue) ContextWindow = contextWindow.Value;
        if (stopSequences != null) StopSequences = stopSequences;
        if (systemPrompt != null) SystemPrompt = systemPrompt;
        if (seed.HasValue) Seed = seed.Value;
        if (frequencyPenalty.HasValue) FrequencyPenalty = frequencyPenalty.Value;
        if (presencePenalty.HasValue) PresencePenalty = presencePenalty.Value;

        MarkUpdated();
    }

    public void UpdateModel(string modelIdentifier, InferenceProviderType providerType)
    {
        ModelIdentifier = modelIdentifier;
        ProviderType = providerType;
        MarkUpdated();
    }

    public void SetAsDefault(bool isDefault)
    {
        IsDefault = isDefault;
        MarkUpdated();
    }

    public void UpdateName(string name)
    {
        Name = name;
        MarkUpdated();
    }
}
