namespace Distributor.Application;

/// <summary>
/// A request with a response.
/// </summary>
/// <typeparam name="TResponse">The type of response produced for this request.</typeparam>
public interface IRequest<out TResponse>;
