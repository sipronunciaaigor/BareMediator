using BareMediator.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BareMediator.Tests;

/// <summary>
/// Integration tests that verify the entire mediator pipeline works correctly
/// </summary>
public class IntegrationTests
{
    [Fact]
    public async Task CompleteFlow_WithQuery_ShouldWorkEndToEnd()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMediator<IntegrationTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        UserDto response = await mediator.Send(new GetUserQuery(Guid.NewGuid(), "John"));

        // Assert
        response.Should().NotBeNull();
        response.Name.Should().Be("John");
    }

    [Fact]
    public async Task CompleteFlow_WithCommand_ShouldWorkEndToEnd()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMediator<IntegrationTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        Guid result = await mediator.Send(new CreateUserCommand("Jane", "jane@example.com"));

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CompleteFlow_WithUnitCommand_ShouldWorkEndToEnd()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMediator<IntegrationTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        Unit result = await mediator.Send(new DeleteUserCommand(Guid.NewGuid()));

        // Assert
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task CompleteFlow_WithDependencyInjection_ShouldInjectDependencies()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddMediator<IntegrationTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        Guid userId = Guid.NewGuid();
        await mediator.Send(new CreateUserWithRepositoryCommand(userId, "Bob"));

        // Act
        UserDto response = await mediator.Send(new GetUserFromRepositoryQuery(userId));

        // Assert
        response.Should().NotBeNull();
        response.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task CompleteFlow_WithMultipleConcurrentRequests_ShouldHandleAll()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMediator<IntegrationTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        List<Task<UserDto>> requests = Enumerable.Range(1, 100)
            .Select(i => mediator.Send(new GetUserQuery(Guid.NewGuid(), $"User{i}")))
            .ToList();

        // Act
        UserDto[] responses = await Task.WhenAll(requests);

        // Assert
        responses.Should().HaveCount(100);
        responses.Select(r => r.Name).Should().OnlyContain(name => name.StartsWith("User"));
    }

    [Fact]
    public async Task CompleteFlow_WithNestedMediation_ShouldWork()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMediator<IntegrationTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        string result = await mediator.Send(new NestedCommand("test"));

        // Assert
        result.Should().Be("NESTED: test");
    }

    [Fact]
    public async Task CompleteFlow_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMediator<IntegrationTests>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        Func<Task<string>> act = async () => await mediator.Send(new LongRunningQuery(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // Test queries and commands
    private record GetUserQuery(Guid Id, string Name) : IRequest<UserDto>;

    private record UserDto(Guid Id, string Name);

    private class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
    {
        public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new UserDto(request.Id, request.Name));
        }
    }

    private record CreateUserCommand(string Name, string Email) : IRequest<Guid>;

    private class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
    {
        public Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Guid.NewGuid());
        }
    }

    private record DeleteUserCommand(Guid Id) : IRequest<Unit>;

    private class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
    {
        public Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }

    // Repository pattern integration
    private interface IUserRepository
    {
        void Add(Guid id, string name);
        string? Get(Guid id);
    }

    private class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, string> _users = new();

        public void Add(Guid id, string name) => _users[id] = name;
        public string? Get(Guid id) => _users.TryGetValue(id, out string? name) ? name : null;
    }

    private record CreateUserWithRepositoryCommand(Guid Id, string Name) : IRequest<Unit>;

    private class CreateUserWithRepositoryCommandHandler : IRequestHandler<CreateUserWithRepositoryCommand, Unit>
    {
        private readonly IUserRepository _repository;

        public CreateUserWithRepositoryCommandHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public Task<Unit> Handle(CreateUserWithRepositoryCommand request, CancellationToken cancellationToken)
        {
            _repository.Add(request.Id, request.Name);
            return Task.FromResult(Unit.Value);
        }
    }

    private record GetUserFromRepositoryQuery(Guid Id) : IRequest<UserDto>;

    private class GetUserFromRepositoryQueryHandler : IRequestHandler<GetUserFromRepositoryQuery, UserDto>
    {
        private readonly IUserRepository _repository;

        public GetUserFromRepositoryQueryHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public Task<UserDto> Handle(GetUserFromRepositoryQuery request, CancellationToken cancellationToken)
        {
            string name = _repository.Get(request.Id) ?? "Unknown";
            return Task.FromResult(new UserDto(request.Id, name));
        }
    }

    // Nested mediation test
    private record NestedCommand(string Value) : IRequest<string>;

    private class NestedCommandHandler : IRequestHandler<NestedCommand, string>
    {
        private readonly IMediator _mediator;

        public NestedCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<string> Handle(NestedCommand request, CancellationToken cancellationToken)
        {
            string innerResult = await _mediator.Send(new InnerQuery(request.Value), cancellationToken);
            return $"NESTED: {innerResult}";
        }
    }

    private record InnerQuery(string Value) : IRequest<string>;

    private class InnerQueryHandler : IRequestHandler<InnerQuery, string>
    {
        public Task<string> Handle(InnerQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.Value);
        }
    }

    // Cancellation test
    private record LongRunningQuery : IRequest<string>;

    private class LongRunningQueryHandler : IRequestHandler<LongRunningQuery, string>
    {
        public async Task<string> Handle(LongRunningQuery request, CancellationToken cancellationToken)
        {
            await Task.Delay(10000, cancellationToken);
            return "completed";
        }
    }
}
