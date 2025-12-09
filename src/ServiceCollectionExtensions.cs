using BareMediator.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BareMediator;

/// <summary>
/// Extension methods for setting up mediator services in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds mediator services to the specified <see cref="IServiceCollection" />.
    /// Scans the provided assemblies for handlers and registers them.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided to scan for handlers", nameof(assemblies));
        }

        services.AddTransient<IMediator, Mediator>();

        foreach (Assembly assembly in assemblies)
        {
            RegisterHandlersFromAssembly(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Adds mediator services to the specified <see cref="IServiceCollection" />.
    /// Scans the assembly containing the specified type for handlers and registers them.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <typeparam name="T">Type from the assembly to scan</typeparam>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMediator<T>(this IServiceCollection services)
    {
        return AddMediator(services, typeof(T).Assembly);
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        Type handlerInterfaceType = typeof(IRequestHandler<,>);

        IEnumerable<Type> handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (Type? implementationType in handlerTypes)
        {
            IEnumerable<Type> interfaces = implementationType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType);

            foreach (Type? interfaceType in interfaces)
            {
                services.AddTransient(interfaceType, implementationType);
            }
        }
    }
}
