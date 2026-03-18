using LocalInference.Application.Abstractions.Inference;
using LocalInference.Application.Abstractions.Summarization;
using Microsoft.Extensions.Logging;

namespace LocalInference.Infrastructure.Summarization;

public class TechnicalSummarizationService : ITechnicalSummarizationService
{
    private readonly IInferenceProviderFactory _providerFactory;
    private readonly ILogger<TechnicalSummarizationService> _logger;

    public TechnicalSummarizationService(
        IInferenceProviderFactory providerFactory,
        ILogger<TechnicalSummarizationService> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<string> SummarizeConversationAsync(
        IReadOnlyList<MessageSummary> messages,
        SummarizationOptions options,
        CancellationToken cancellationToken = default)
    {
        var conversationText = string.Join("\n\n", messages.Select(m => $"[{m.Role}]: {m.Content}"));
        var originalTokens = EstimateTokens(conversationText);

        var prompt = $@"Summarize the following conversation concisely. Focus on key facts, technical details, and important decisions.
Target length: approximately {options.TargetTokenCount} tokens.

Conversation:
{conversationText}

Summary:";

        var request = new InferenceRequest
        {
            ModelIdentifier = "llama3.2",
            Messages = new[]
            {
                ChatMessage.System("You are a technical summarization assistant. Create concise, accurate summaries that preserve key technical details and facts."),
                ChatMessage.User(prompt)
            },
            Temperature = 0.3,
            MaxTokens = options.TargetTokenCount
        };

        var inferenceProvider = _providerFactory.GetProvider("Ollama");
        var response = await inferenceProvider.CompleteAsync(request, cancellationToken);
        return response.Content.Trim();
    }

    public async Task<string> CompressTechnicalContextAsync(
        string context,
        CompressionOptions options,
        CancellationToken cancellationToken = default)
    {
        var originalTokens = EstimateTokens(context);

        var prompt = $@"Compress the following technical context while preserving all key information, code structure, and technical details.
Target length: approximately {options.TargetTokenCount} tokens.

Context:
{context}

Compressed version:";

        var request = new InferenceRequest
        {
            ModelIdentifier = "llama3.2",
            Messages = new[]
            {
                ChatMessage.System("You are a technical compression assistant. Compress text while preserving code blocks, technical terms, and key information."),
                ChatMessage.User(prompt)
            },
            Temperature = 0.2,
            MaxTokens = options.TargetTokenCount
        };

        var inferenceProvider = _providerFactory.GetProvider("Ollama");
        var response = await inferenceProvider.CompleteAsync(request, cancellationToken);
        return response.Content.Trim();
    }

    public async Task<TechnicalSummary> SummarizeForRetrievalAsync(
        string content,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        var originalTokens = EstimateTokens(content);

        var prompt = $@"Analyze this {documentType} document and provide:
1. A concise summary (2-3 sentences)
2. Key technical points (bullet points)
3. Important technical terms and concepts

Document:
{content}

Format your response as:
SUMMARY: [summary]
KEY_POINTS: [bullet points]
TERMS: [comma-separated terms]";

        var request = new InferenceRequest
        {
            ModelIdentifier = "llama3.2",
            Messages = new[]
            {
                ChatMessage.System("You are a technical document analyzer. Extract key information for retrieval purposes."),
                ChatMessage.User(prompt)
            },
            Temperature = 0.3,
            MaxTokens = 500
        };

        var inferenceProvider = _providerFactory.GetProvider("Ollama");
        var response = await inferenceProvider.CompleteAsync(request, cancellationToken);
        var parsed = ParseTechnicalSummary(response.Content);

        var compressedTokens = EstimateTokens(parsed.Summary) +
                               EstimateTokens(parsed.KeyPoints) +
                               EstimateTokens(parsed.TechnicalTerms);

        return new TechnicalSummary
        {
            Summary = parsed.Summary,
            KeyPoints = parsed.KeyPoints,
            TechnicalTerms = parsed.TechnicalTerms,
            OriginalTokenCount = originalTokens,
            CompressedTokenCount = compressedTokens,
            CompressionRatio = originalTokens > 0 ? (double)compressedTokens / originalTokens : 0
        };
    }

    private (string Summary, string KeyPoints, string TechnicalTerms) ParseTechnicalSummary(string content)
    {
        var summary = "";
        var keyPoints = "";
        var terms = "";

        var lines = content.Split('\n');
        var currentSection = "";

        foreach (var line in lines)
        {
            if (line.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "summary";
                summary = line.Substring(8).Trim();
            }
            else if (line.StartsWith("KEY_POINTS:", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "keypoints";
                keyPoints = line.Substring(11).Trim();
            }
            else if (line.StartsWith("TERMS:", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "terms";
                terms = line.Substring(6).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                switch (currentSection)
                {
                    case "summary":
                        summary += " " + line.Trim();
                        break;
                    case "keypoints":
                        keyPoints += "\n" + line.Trim();
                        break;
                    case "terms":
                        terms += " " + line.Trim();
                        break;
                }
            }
        }

        return (summary.Trim(), keyPoints.Trim(), terms.Trim());
    }

    private int EstimateTokens(string text)
    {
        return (int)(text.Length / 4.0);
    }
}
