namespace LocalInference.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class SessionNotFoundException : DomainException
{
    public Guid SessionId { get; }

    public SessionNotFoundException(Guid sessionId)
        : base($"Session with ID '{sessionId}' was not found.")
    {
        SessionId = sessionId;
    }
}

public class InferenceConfigNotFoundException : DomainException
{
    public Guid ConfigId { get; }

    public InferenceConfigNotFoundException(Guid configId)
        : base($"Inference configuration with ID '{configId}' was not found.")
    {
        ConfigId = configId;
    }
}

public class TokenBudgetExceededException : DomainException
{
    public int RequestedTokens { get; }
    public int AvailableTokens { get; }

    public TokenBudgetExceededException(int requestedTokens, int availableTokens)
        : base($"Token budget exceeded. Requested: {requestedTokens}, Available: {availableTokens}")
    {
        RequestedTokens = requestedTokens;
        AvailableTokens = availableTokens;
    }
}

public class InferenceProviderException : DomainException
{
    public string ProviderName { get; }

    public InferenceProviderException(string providerName, string message)
        : base($"Inference provider '{providerName}' error: {message}")
    {
        ProviderName = providerName;
    }
}
