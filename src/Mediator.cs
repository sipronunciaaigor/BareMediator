using BareMediator.Abstractions;
using System.Collections.Concurrent;
using System.Reflection;

namespace BareMediator;

/// <summary>
/// Default implementation of IMediator
/// </summary>
public sealed class Mediator(IServiceProvider serviceProvider)
    : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider
        ?? throw new ArgumentNullException(nameof(serviceProvider));

    private static readonly ConcurrentDictionary<Type, MethodInfo> _handleMethodCache = new();

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Type requestType = request.GetType();
        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        object handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for request type {requestType.Name}");

        MethodInfo handleMethod = _handleMethodCache.GetOrAdd(handlerType, static type => type.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handle method not found on {type.Name}"));

        Task<TResponse> task = handleMethod.Invoke(handler, [request, cancellationToken]) as Task<TResponse>
            ?? throw new InvalidOperationException($"Handler for {requestType.Name} did not return expected Task<{typeof(TResponse).Name}>");

        return await task;
    }
}
