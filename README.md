# BareMediator

A super lightweight mediator pattern library with no frills. 🐻

A simple implementation for .NET that provides request/handler pattern capabilities without the overhead of a full-featured library.

**NOTE:** BareMediator is free for individuals and companies with less than USD 500,000 in ARR. Above that threshold, check [licensing](LICENSE) and [pricing](PRICING.md).


## Features

### What it is
- Simple request/response pattern
- Automatic handler discovery and registration
- Dependency injection integration
- Async/await support
- Minimal dependencies

### What it is not
- No notifications/events
- No pipeline behaviors
- No streaming

## Usage
```csharp
decimal fee = PricingCalculator.CalculateAnnualFee(1_500_000m); // 18

## Installation

Install the nuget package via the .NET CLI:

```bash
dotnet add package BareMediator
```

## Usage

### 1. Register services

```csharp
services.AddMediator<Program>(); // Scans the assembly containing Program

// or

services.AddMediator(typeof(Program).Assembly, typeof(OtherType).Assembly);
```
**Note:** BareMediator is instantiated as a transient service.

### 2. Define request and response

```csharp
using BareMediator;

public record GetUserRequest
    : IRequest<GetUserResponse>
{
    public Guid UserId { get; init; }
}

public record GetUserResponse
{
    public Guid UserId { get; init; }
    public string Name { get; init; }
}
```

### 3. Define a handler

```csharp
using BareMediator;

public class GetUserRequestHandler
    : IRequestHandler<GetUserRequest, GetUserResponse>
{
    public async Task<GetUserResponse> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        // Your logic here
        return new GetUserResponse
        {
            UserId = request.UserId,
            Name = "John Doe"
        };
    }
}
```

### 4. Use the mediator

```csharp
public class UsersController(IMediator mediator)
    : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        GetUserRequest request = new { UserId = id };

        GetUserResponse response = await _mediator.Send(request);

        return Ok(response);
    }
}
```

## Commands without return values

For commands that don't return data (e.g., `DeleteUserCommand`, `UpdateSettingsCommand`), use the `Unit` type instead of `void`:

```csharp
// Command
public record DeleteUserCommand(Guid UserId) : IRequest<Unit>;

// Handler
public class DeleteUserCommandHandler(IUserRepository repository)
    : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _repository = repository;

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.UserId, cancellationToken);
        return Unit.Value; // Represents successful completion
    }
}

// Usage
await _mediator.Send(new DeleteUserCommand(userId));
```

**Why `Unit` instead of `void`?**
- C# doesn't support `void` as a generic type parameter
- `Unit` is a singleton value type that represents "no data"
- All `Unit` instances are equal and have no meaningful state
- Compatible with MediatR's `Unit` type for easy migration

## Why BareMediator?

MediatR is a fantastic library, but if you only need request/handler capabilities, you're pulling in a much larger library with features you don't use.

BareMediator provides just the essentials:

- **Lightweight**: Nearly 7x smaller than MediatR
- **Fast**: No pipeline overhead
- **Simple**: Only 6 files, <200 lines of code: easy to understand and debug
- **Compatible**: Drop-in replacement for basic MediatR usage

## Migration from MediatR

Everywhere simply replace:
```csharp
// Before
using MediatR;

// After
using BareMediator;
```

In your `Program.cs` replace also:
```csharp
// Before
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// After
services.AddMediator<Program>();
```

All your existing `IRequest<T>`, `IRequestHandler<TRequest, TResponse>`, and `IMediator` code will work as-is!
