using LocalInference.Application.Prompting;

namespace LocalInference.Application.Tests.Prompting;

public class ContextBudgetTests
{
    [Fact]
    public void Constructor_AllocatesCorrectSlices_WhenGivenRatios()
    {
        // Arrange
        int totalTokens = 1000;

        // Act
        var budget = new ContextBudget(totalTokens, 0.2, 0.3, 0.3, 0.2);

        // Assert
        Assert.Equal(200, budget.SystemSlice);
        Assert.Equal(300, budget.RetrievalSlice);
        Assert.Equal(300, budget.HistorySlice);
        Assert.Equal(200, budget.SummarySlice);
        Assert.Equal(1000, budget.TotalTokens);
    }

    [Fact]
    public void Constructor_AdjustsForRounding_WhenSlicesExceedTotal()
    {
        // Arrange
        int totalTokens = 100; // Small number to make rounding issues apparent

        // Act
        var budget = new ContextBudget(totalTokens, 0.33, 0.33, 0.34, 0.0); // 33+33+34 = 100%, but with rounding...

        // Assert
        Assert.Equal(totalTokens, budget.SystemSlice + budget.RetrievalSlice + budget.HistorySlice + budget.SummarySlice);
    }

    [Fact]
    public void AdjustBasedOnUsage_RedistributesUnusedTokens_WhenSliceUnderUtilized()
    {
        // Arrange
        var budget = new ContextBudget(1000, 0.5, 0.3, 0.1, 0.1); // 500, 300, 100, 100
        int systemUsed = 400; // 100 unused
        int retrievalUsed = 300; // 0 unused
        int historyUsed = 50; // 50 unused
        int summaryUsed = 100; // 0 unused
        // Total unused: 150

        // Act
        var adjusted = budget.AdjustBasedOnUsage(systemUsed, retrievalUsed, historyUsed, summaryUsed);

        // Assert - system should get some of the unused tokens from history
        Assert.InRange(adjusted.SystemSlice, 500, 600); // Should be between original and original+history unused
        Assert.InRange(adjusted.HistorySlice, 50, 150); // Should be between original and original-allocated
    }

    [Fact]
    public void AdjustBasedOnUsage_IncreasesSlice_WhenSliceOverUtilizedAndUnavailableElsewhere()
    {
        // Arrange
        var budget = new ContextBudget(1000, 0.4, 0.4, 0.1, 0.1); // 400, 400, 100, 100
        int systemUsed = 450; // 50 deficit
        int retrievalUsed = 350; // 50 unused
        int historyUsed = 100; // 0 unused
        int summaryUsed = 100; // 0 unused

        // Act
        var adjusted = budget.AdjustBasedOnUsage(systemUsed, retrievalUsed, historyUsed, summaryUsed);

        // Assert - system should increase, retrieval should decrease
        Assert.InRange(adjusted.SystemSlice, 400, 500); // Should increase to cover deficit
        Assert.InRange(adjusted.RetrievalSlice, 300, 400); // Should decrease to give to system
    }

    [Fact]
    public void AdjustBasedOnUsage_ReturnsSameBudget_WhenNoRedistributionNeeded()
    {
        // Arrange
        var budget = new ContextBudget(1000, 0.25, 0.25, 0.25, 0.25);
        int systemUsed = 200;
        int retrievalUsed = 250;
        int historyUsed = 200;
        int summaryUsed = 150;

        // Act
        var adjusted = budget.AdjustBasedOnUsage(systemUsed, retrievalUsed, historyUsed, summaryUsed);

        // Assert - should be identical since no slice exceeded its allocation
        Assert.Equal(budget.SystemSlice, adjusted.SystemSlice);
        Assert.Equal(budget.RetrievalSlice, adjusted.RetrievalSlice);
        Assert.Equal(budget.HistorySlice, adjusted.HistorySlice);
        Assert.Equal(budget.SummarySlice, adjusted.SummarySlice);
    }
}