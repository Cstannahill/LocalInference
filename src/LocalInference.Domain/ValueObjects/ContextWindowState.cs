namespace LocalInference.Domain.ValueObjects;

public sealed record ContextWindowState
{
    public int TotalTokens { get; init; }
    public int SystemTokens { get; init; }
    public int ContextTokens { get; init; }
    public int OutputTokens { get; init; }
    public int AvailableTokens { get; init; }
    public double UtilizationRatio { get; init; }
    public bool IsWithinBudget { get; init; }
    public int RecommendedTrimCount { get; init; }

    public static ContextWindowState Calculate(int totalTokens, int maxTokens, int systemTokens, int outputTokens)
    {
        var contextTokens = totalTokens - systemTokens;
        var availableTokens = maxTokens - totalTokens;
        var utilizationRatio = maxTokens > 0 ? (double)totalTokens / maxTokens : 0;
        var isWithinBudget = totalTokens <= maxTokens;
        var recommendedTrimCount = 0;

        if (!isWithinBudget)
        {
            var excessTokens = totalTokens - maxTokens;
            recommendedTrimCount = (int)Math.Ceiling(excessTokens / 100.0);
        }

        return new ContextWindowState
        {
            TotalTokens = totalTokens,
            SystemTokens = systemTokens,
            ContextTokens = contextTokens,
            OutputTokens = outputTokens,
            AvailableTokens = availableTokens,
            UtilizationRatio = utilizationRatio,
            IsWithinBudget = isWithinBudget,
            RecommendedTrimCount = recommendedTrimCount
        };
    }
}
