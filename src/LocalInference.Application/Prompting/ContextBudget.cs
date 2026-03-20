namespace LocalInference.Application.Prompting;

/// <summary>
/// Represents a budget for allocating tokens across different parts of the context.
/// </summary>
public class ContextBudget
{
    /// <summary>
    /// Total available tokens for the context.
    /// </summary>
    public int TotalTokens { get; }

    /// <summary>
    /// Tokens allocated for the system slice (high priority).
    /// </summary>
    public int SystemSlice { get; private set; }

    /// <summary>
    /// Tokens allocated for the retrieval slice (medium priority).
    /// </summary>
    public int RetrievalSlice { get; private set; }

    /// <summary>
    /// Tokens allocated for the history slice (medium priority).
    /// </summary>
    public int HistorySlice { get; private set; }

    /// <summary>
    /// Tokens allocated for the summary slice (medium priority).
    /// </summary>
    public int SummarySlice { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextBudget"/> class.
    /// </summary>
    /// <param name="totalTokens">Total available tokens.</param>
    /// <param name="systemSliceRatio">Ratio of total tokens for system slice (default 0.15).</param>
    /// <param name="retrievalSliceRatio">Ratio of total tokens for retrieval slice (default 0.25).</param>
    /// <param name="historySliceRatio">Ratio of total tokens for history slice (default 0.35).</param>
    /// <param name="summarySliceRatio">Ratio of total tokens for summary slice (default 0.25).</param>
    public ContextBudget(int totalTokens,
                         double systemSliceRatio = 0.15,
                         double retrievalSliceRatio = 0.25,
                         double historySliceRatio = 0.35,
                         double summarySliceRatio = 0.25)
    {
        TotalTokens = totalTokens;
        SystemSlice = (int)(totalTokens * systemSliceRatio);
        RetrievalSlice = (int)(totalTokens * retrievalSliceRatio);
        HistorySlice = (int)(totalTokens * historySliceRatio);
        SummarySlice = (int)(totalTokens * summarySliceRatio);

        // Ensure we don't exceed total tokens due to rounding
        int allocated = SystemSlice + RetrievalSlice + HistorySlice + SummarySlice;
        if (allocated > TotalTokens)
        {
            // Reduce the lowest priority slice (summary) to fit
            int excess = allocated - TotalTokens;
            SummarySlice = Math.Max(0, SummarySlice - excess);
        }
    }

    /// <summary>
    /// Adjusts the slices based on actual usage, allowing reallocation from lower to higher priority slices.
    /// </summary>
    /// <param name="systemUsed">Tokens actually used by system slice.</param>
    /// <param name="retrievalUsed">Tokens actually used by retrieval slice.</param>
    /// <param name="historyUsed">Tokens actually used by history slice.</param>
    /// <param name="summaryUsed">Tokens actually used by summary slice.</param>
    /// <returns>A new ContextBudget with adjusted slices based on usage.</returns>
    public ContextBudget AdjustBasedOnUsage(int systemUsed, int retrievalUsed, int historyUsed, int summaryUsed)
    {
        // Calculate unused tokens from each slice
        int systemUnused = Math.Max(0, SystemSlice - systemUsed);
        int retrievalUnused = Math.Max(0, RetrievalSlice - retrievalUsed);
        int historyUnused = Math.Max(0, HistorySlice - historyUsed);
        int summaryUnused = Math.Max(0, SummarySlice - summaryUsed);

        // Total unused tokens
        int totalUnused = systemUnused + retrievalUnused + historyUnused + summaryUnused;

        // If there's no unused tokens, return the original budget
        if (totalUnused == 0)
            return this;

        // Redistribute unused tokens to slices that exceeded their allocation
        int systemDeficit = Math.Max(0, systemUsed - SystemSlice);
        int retrievalDeficit = Math.Max(0, retrievalUsed - RetrievalSlice);
        int historyDeficit = Math.Max(0, historyUsed - HistorySlice);
        int summaryDeficit = Math.Max(0, summaryUsed - SummarySlice);

        int totalDeficit = systemDeficit + retrievalDeficit + historyDeficit + summaryDeficit;

        // If there's no deficit, return the original budget
        if (totalDeficit == 0)
            return this;

        // Distribute unused tokens to cover deficits, prioritizing higher slices
        int newSystemSlice = SystemSlice + Math.Min(systemUnused, systemDeficit);
        int remainingUnused = systemUnused - Math.Min(systemUnused, systemDeficit);

        int newRetrievalSlice = RetrievalSlice + Math.Min(retrievalUnused + remainingUnused, retrievalDeficit);
        remainingUnused = (retrievalUnused + remainingUnused) - Math.Min(retrievalUnused + remainingUnused, retrievalDeficit);

        int newHistorySlice = HistorySlice + Math.Min(historyUnused + remainingUnused, historyDeficit);
        remainingUnused = (historyUnused + remainingUnused) - Math.Min(historyUnused + remainingUnused, historyDeficit);

        int newSummarySlice = SummarySlice + Math.Min(summaryUnused + remainingUnused, summaryDeficit);

        return new ContextBudget(TotalTokens,
                                (double)newSystemSlice / TotalTokens,
                                (double)newRetrievalSlice / TotalTokens,
                                (double)newHistorySlice / TotalTokens,
                                (double)newSummarySlice / TotalTokens);
    }
}