using BareMediator.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace BareMediator.Tests;

public class MediatorTests
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Mediator> act = () => new Mediator(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public async Task Send_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        Mediator mediator = new(serviceProvider);

        // Act
        Func<Task<string>> act = async () => await mediator.Send<string>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Send_WithNoHandlerRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);

        Mediator mediator = new(serviceProvider);
        TestRequest request = new();

        // Act
        Func<Task<TestResponse>> act = async () => await mediator.Send(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No handler registered for request type TestRequest");
    }

    [Fact]
    public async Task Send_WithRegisteredHandler_ShouldInvokeHandlerAndReturnResponse()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddTransient<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mediator mediator = new(serviceProvider);
        TestRequest request = new() { Value = "test" };

        // Act
        TestResponse response = await mediator.Send(request);

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be("Handled: test");
    }

    [Fact]
    public async Task Send_WithMultipleRequests_ShouldUseCorrectHandler()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddTransient<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddTransient<IRequestHandler<AnotherRequest, AnotherResponse>, AnotherRequestHandler>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mediator mediator = new(serviceProvider);
        TestRequest request1 = new() { Value = "first" };
        AnotherRequest request2 = new() { Number = 42 };

        // Act
        TestResponse response1 = await mediator.Send(request1);
        AnotherResponse response2 = await mediator.Send(request2);

        // Assert
        response1.Result.Should().Be("Handled: first");
        response2.Value.Should().Be(42);
    }

    [Fact]
    public async Task Send_WithCancellationToken_ShouldPassTokenToHandler()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddTransient<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mediator mediator = new(serviceProvider);
        TestRequest request = new() { Value = "test" };
        CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        Func<Task<TestResponse>> act = async () => await mediator.Send(request, cts.Token);

        // Assert - Reflection wraps in TargetInvocationException
        await act.Should().ThrowAsync<Exception>()
            .Where(ex => ex is System.Reflection.TargetInvocationException &&
                         ex.InnerException is OperationCanceledException);
    }

    [Fact]
    public async Task Send_WithUnitResponse_ShouldWork()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddTransient<IRequestHandler<UnitRequest, Unit>, UnitRequestHandler>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mediator mediator = new(serviceProvider);
        UnitRequest request = new();

        // Act
        Unit response = await mediator.Send(request);

        // Assert
        response.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Send_MultipleCallsWithSameRequestType_ShouldCacheReflection()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddTransient<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mediator mediator = new(serviceProvider);

        // Act - Multiple calls should reuse cached MethodInfo
        TestResponse response1 = await mediator.Send(new TestRequest { Value = "first" });
        TestResponse response2 = await mediator.Send(new TestRequest { Value = "second" });
        TestResponse response3 = await mediator.Send(new TestRequest { Value = "third" });

        // Assert
        response1.Result.Should().Be("Handled: first");
        response2.Result.Should().Be("Handled: second");
        response3.Result.Should().Be("Handled: third");
    }

    [Fact]
    public async Task Send_WithDifferentResponseTypes_ShouldWork()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddTransient<IRequestHandler<IntRequest, int>, IntRequestHandler>();
        services.AddTransient<IRequestHandler<StringRequest, string>, StringRequestHandler>();
        services.AddTransient<IRequestHandler<BoolRequest, bool>, BoolRequestHandler>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mediator mediator = new(serviceProvider);

        // Act
        int intResult = await mediator.Send(new IntRequest { Value = 42 });
        string stringResult = await mediator.Send(new StringRequest { Text = "hello" });
        bool boolResult = await mediator.Send(new BoolRequest { Flag = true });

        // Assert
        intResult.Should().Be(42);
        stringResult.Should().Be("hello");
        boolResult.Should().BeTrue();
    }

    // Test request/response types
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
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new TestResponse { Result = $"Handled: {request.Value}" });
        }
    }

    private record AnotherRequest : IRequest<AnotherResponse>
    {
        public int Number { get; init; }
    }

    private record AnotherResponse
    {
        public int Value { get; init; }
    }

    private class AnotherRequestHandler : IRequestHandler<AnotherRequest, AnotherResponse>
    {
        public Task<AnotherResponse> Handle(AnotherRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AnotherResponse { Value = request.Number });
        }
    }

    private record UnitRequest : IRequest<Unit>;

    private class UnitRequestHandler : IRequestHandler<UnitRequest, Unit>
    {
        public Task<Unit> Handle(UnitRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }

    private record IntRequest : IRequest<int>
    {
        public int Value { get; init; }
    }

    private class IntRequestHandler : IRequestHandler<IntRequest, int>
    {
        public Task<int> Handle(IntRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.Value);
        }
    }

    private record StringRequest : IRequest<string>
    {
        public string Text { get; init; } = string.Empty;
    }

    private class StringRequestHandler : IRequestHandler<StringRequest, string>
    {
        public Task<string> Handle(StringRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.Text);
        }
    }

    private record BoolRequest : IRequest<bool>
    {
        public bool Flag { get; init; }
    }

    private class BoolRequestHandler : IRequestHandler<BoolRequest, bool>
    {
        public Task<bool> Handle(BoolRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.Flag);
        }
    }
}
