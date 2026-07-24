namespace Distributor.Application;

/// <summary>
/// A handler for a request with a response.
/// </summary>
/// <typeparam name="TRequest">The type of request to be handled.</typeparam>
/// <typeparam name="TResponse">The type of response to be produced.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles a request, producing a response to the request.
    /// </summary>
    /// <param name="request">The request to be handled.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>The response to the request.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken token = default);
}
