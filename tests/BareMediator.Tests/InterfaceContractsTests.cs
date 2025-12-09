using BareMediator.Abstractions;
using FluentAssertions;
using System.Reflection;

namespace BareMediator.Tests;

public class InterfaceContractsTests
{
    [Fact]
    public void IRequest_ShouldBeCovariant()
    {
        // Arrange & Act
        IRequest<object> request = new DerivedRequest();

        // Assert
        request.Should().NotBeNull();
        request.Should().BeAssignableTo<IRequest<string>>();
    }

    [Fact]
    public void IRequestHandler_ShouldHaveCorrectGenericConstraint()
    {
        // Arrange
        Type handlerType = typeof(IRequestHandler<,>);

        // Act
        Type[] constraints = handlerType.GetGenericArguments()[0].GetGenericParameterConstraints();

        // Assert
        constraints.Should().HaveCount(1);
        constraints[0].Should().Match(t =>
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    [Fact]
    public void IRequestHandler_TRequest_ShouldBeContravariant()
    {
        // This test verifies the 'in' modifier on TRequest parameter
        Type handlerType = typeof(IRequestHandler<,>);
        Type tRequestParameter = handlerType.GetGenericArguments()[0];

        // Assert
        tRequestParameter.GenericParameterAttributes
            .HasFlag(GenericParameterAttributes.Contravariant)
            .Should().BeTrue();
    }

    [Fact]
    public void IMediator_Send_ShouldBeGeneric()
    {
        // Arrange
        Type mediatorType = typeof(IMediator);
        MethodInfo? sendMethod = mediatorType.GetMethod(nameof(IMediator.Send));

        // Assert
        sendMethod.Should().NotBeNull();
        sendMethod!.IsGenericMethod.Should().BeTrue();
        sendMethod.GetGenericArguments().Should().HaveCount(1);
    }

    [Fact]
    public void IMediator_Send_ShouldReturnTask()
    {
        // Arrange
        Type mediatorType = typeof(IMediator);
        MethodInfo? sendMethod = mediatorType.GetMethod(nameof(IMediator.Send));

        // Assert
        sendMethod.Should().NotBeNull();
        sendMethod!.ReturnType.IsGenericType.Should().BeTrue();
        sendMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(Task<>));
    }

    [Fact]
    public void IRequestHandler_Handle_ShouldAcceptCancellationToken()
    {
        // Arrange
        Type handlerType = typeof(IRequestHandler<,>);
        MethodInfo? handleMethod = handlerType.GetMethod(nameof(IRequestHandler<,>.Handle));

        // Assert
        handleMethod.Should().NotBeNull();
        ParameterInfo[] parameters = handleMethod!.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[1].ParameterType.Should().Be(typeof(CancellationToken));
    }

    [Fact]
    public void ConcreteHandler_ShouldImplementInterface()
    {
        // Arrange
        TestHandler handler = new();

        // Assert
        handler.Should().BeAssignableTo<IRequestHandler<TestRequest, string>>();
    }

    [Fact]
    public async Task ConcreteHandler_ShouldBeCallable()
    {
        // Arrange
        TestHandler handler = new();
        TestRequest request = new() { Value = "test" };

        // Act
        string result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be("test");
    }

    // Test types
    private record TestRequest : IRequest<string>
    {
        public string Value { get; init; } = string.Empty;
    }

    private class TestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.Value);
        }
    }

    private record DerivedRequest : IRequest<string>
    {
        public string Value { get; init; } = string.Empty;
    }
}
