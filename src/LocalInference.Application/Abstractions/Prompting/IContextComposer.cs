using LocalInference.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace LocalInference.Application.Abstractions.Prompting
{
    /// <summary>
    /// Composes the final prompt for the LLM by allocating tokens across different context slices
    /// based on priority and availability.
    /// </summary>
    public interface IContextComposer
    {
        /// <summary>
        /// Composes the prompt for a given session and user message.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userMessage">The current user message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The composed prompt ready for LLM inference.</returns>
        Task<string> ComposePromptAsync(Guid sessionId, string userMessage, CancellationToken cancellationToken = default);
    }
}