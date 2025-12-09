using BareMediator.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BareMediator.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMediator_WithNullAssemblies_ShouldThrowArgumentException()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        Func<IServiceCollection> act = () => services.AddMediator(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one assembly must be provided to scan for handlers*")
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AddMediator_WithEmptyAssemblies_ShouldThrowArgumentException()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        Func<IServiceCollection> act = () => services.AddMediator([]);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one assembly must be provided to scan for handlers*")
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AddMediator_WithValidAssembly_ShouldRegisterMediator()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMediator(typeof(ServiceCollectionExtensionsTests).Assembly);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IMediator? mediator = serviceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
        mediator.Should().BeOfType<Mediator>();
    }

    [Fact]
    public void AddMediator_WithValidAssembly_ShouldRegisterHandlers()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMediator(typeof(ServiceCollectionExtensionsTests).Assembly);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IRequestHandler<TestRequest, TestResponse>? handler = serviceProvider.GetService<IRequestHandler<TestRequest, TestResponse>>();
        handler.Should().NotBeNull();
        handler.Should().BeOfType<TestRequestHandler>();
    }

    [Fact]
    public void AddMediator_WithMultipleAssemblies_ShouldRegisterHandlersFromAll()
    {
        // Arrange
        ServiceCollection services = new();
        Assembly assembly1 = typeof(ServiceCollectionExtensionsTests).Assembly;
        Assembly assembly2 = typeof(IMediator).Assembly;

        // Act
        services.AddMediator(assembly1, assembly2);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IMediator? mediator = serviceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddMediator_WithGenericType_ShouldRegisterHandlersFromAssembly()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMediator<ServiceCollectionExtensionsTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IMediator? mediator = serviceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();

        IRequestHandler<TestRequest, TestResponse>? handler = serviceProvider.GetService<IRequestHandler<TestRequest, TestResponse>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AddMediator_ShouldRegisterMediatorAsTransient()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMediator<ServiceCollectionExtensionsTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IMediator? mediator1 = serviceProvider.GetService<IMediator>();
        IMediator? mediator2 = serviceProvider.GetService<IMediator>();

        mediator1.Should().NotBeSameAs(mediator2);
    }

    [Fact]
    public void AddMediator_ShouldRegisterHandlersAsTransient()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMediator<ServiceCollectionExtensionsTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IRequestHandler<TestRequest, TestResponse>? handler1 = serviceProvider.GetService<IRequestHandler<TestRequest, TestResponse>>();
        IRequestHandler<TestRequest, TestResponse>? handler2 = serviceProvider.GetService<IRequestHandler<TestRequest, TestResponse>>();

        handler1.Should().NotBeSameAs(handler2);
    }

    [Fact]
    public void AddMediator_WithHandlerImplementingMultipleInterfaces_ShouldRegisterAll()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMediator<ServiceCollectionExtensionsTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IRequestHandler<MultiRequest1, string>? handler1 = serviceProvider.GetService<IRequestHandler<MultiRequest1, string>>();
        IRequestHandler<MultiRequest2, int>? handler2 = serviceProvider.GetService<IRequestHandler<MultiRequest2, int>>();

        handler1.Should().NotBeNull();
        handler2.Should().NotBeNull();
        handler1.Should().BeOfType<MultiRequestHandler>();
        handler2.Should().BeOfType<MultiRequestHandler>();
    }

    [Fact]
    public void AddMediator_ShouldNotRegisterAbstractHandlers()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMediator<ServiceCollectionExtensionsTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(s =>
            s.ImplementationType == typeof(AbstractHandler));
        descriptor.Should().BeNull();
    }

    [Fact]
    public void AddMediator_ShouldNotRegisterInterfaceHandlers()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMediator<ServiceCollectionExtensionsTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        // The IRequestHandler itself should not be registered as a handler
        IEnumerable<ServiceDescriptor> allHandlers = services.Where(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

        allHandlers.Should().NotBeEmpty();
        allHandlers.All(s => s.ImplementationType?.IsClass == true).Should().BeTrue();
    }

    [Fact]
    public void AddMediator_ReturnsSameServiceCollection_ForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddMediator<ServiceCollectionExtensionsTests>();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public async Task AddMediator_IntegrationTest_ShouldWorkEndToEnd()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMediator<ServiceCollectionExtensionsTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        TestResponse response = await mediator.Send(new TestRequest { Value = "integration" });

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be("Handled: integration");
    }

    // Test types for scanning
    private record TestRequest : IRequest<TestResponse>
    {
        public string Value { get; init; } = string.Empty;
    }

    private record TestResponse
    {
        public string Result { get; init; } = string.Empty;
    }

    private class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TestResponse { Result = $"Handled: {request.Value}" });
        }
    }

    // Handler implementing multiple interfaces
    private record MultiRequest1 : IRequest<string>;
    private record MultiRequest2 : IRequest<int>;

    private class MultiRequestHandler :
        IRequestHandler<MultiRequest1, string>,
        IRequestHandler<MultiRequest2, int>
    {
        public Task<string> Handle(MultiRequest1 request, CancellationToken cancellationToken)
        {
            return Task.FromResult("multi1");
        }

        public Task<int> Handle(MultiRequest2 request, CancellationToken cancellationToken)
        {
            return Task.FromResult(42);
        }
    }

    // Abstract handler (should not be registered)
    private abstract class AbstractHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public abstract Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken);
    }
}
