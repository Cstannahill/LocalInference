namespace LocalInference.Domain.ValueObjects;

public sealed record TokenBudget
{
    public int TotalBudget { get; }
    public int ReservedForOutput { get; }
    public int ReservedForSystem { get; }
    public int AvailableForContext => TotalBudget - ReservedForOutput - ReservedForSystem;
    public int EffectiveContextWindow => AvailableForContext > 0 ? AvailableForContext : 0;

    public TokenBudget(int totalBudget, int reservedForOutput = 2048, int reservedForSystem = 512)
    {
        if (totalBudget <= 0)
            throw new ArgumentException("Total budget must be positive", nameof(totalBudget));

        TotalBudget = totalBudget;
        ReservedForOutput = reservedForOutput;
        ReservedForSystem = reservedForSystem;
    }

    public int CalculateSlidingWindowSize(int messageCount)
    {
        if (messageCount == 0)
            return 0;

        var avgMessageSize = EffectiveContextWindow / Math.Max(messageCount, 1);
        return Math.Min(avgMessageSize * 2, EffectiveContextWindow);
    }

    public bool CanFit(int tokenCount)
    {
        return tokenCount <= EffectiveContextWindow;
    }

    public static TokenBudget Default => new(8192);
    public static TokenBudget Large => new(32768);
    public static TokenBudget ExtraLarge => new(128000);
}
