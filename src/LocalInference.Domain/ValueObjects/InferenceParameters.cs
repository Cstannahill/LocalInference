namespace LocalInference.Domain.ValueObjects;

public sealed record InferenceParameters
{
    public double Temperature { get; init; } = 0.7;
    public double TopP { get; init; } = 0.9;
    public double? TopK { get; init; }
    public double? RepeatPenalty { get; init; }
    public int? MaxTokens { get; init; }
    public int? Seed { get; init; }
    public double? FrequencyPenalty { get; init; }
    public double? PresencePenalty { get; init; }
    public IReadOnlyList<string> StopSequences { get; init; } = Array.Empty<string>();

    public static InferenceParameters Default => new();
}
