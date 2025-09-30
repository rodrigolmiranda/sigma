using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sigma.Application.Behaviors;
using Sigma.Application.Contracts;
using Sigma.Application.Services;
using Xunit;

namespace Sigma.Application.Tests.Services;

public class CommandDispatcherTests
{
    public class TestCommand : ICommand<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestCommandWithoutResult : ICommand
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestCommandHandler : ICommandHandler<TestCommand, string>
    {
        public Task<Result<string>> HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<string>.Success($"Handled: {command.Value}"));
        }
    }

    public class TestCommandWithoutResultHandler : ICommandHandler<TestCommandWithoutResult>
    {
        public Task<Result> HandleAsync(TestCommandWithoutResult command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }

    [Fact]
    public async Task DispatchAsync_WithRegisteredHandler_ShouldExecuteHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<TestCommand, string>, TestCommandHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new CommandDispatcher(serviceProvider, new List<ICommandBehavior>());
        var command = new TestCommand { Value = "test" };

        // Act
        var result = await dispatcher.DispatchAsync<TestCommand, string>(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Handled: test", result.Value);
    }

    [Fact]
    public async Task DispatchAsync_WithoutRegisteredHandler_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new CommandDispatcher(serviceProvider, new List<ICommandBehavior>());
        var command = new TestCommand { Value = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync<TestCommand, string>(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DispatchAsync_WithBehavior_ShouldExecuteBehaviorPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<TestCommand, string>, TestCommandHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var behaviorMock = new Mock<ICommandBehavior>();
        behaviorMock
            .Setup(b => b.HandleAsync<TestCommand, string>(
                It.IsAny<TestCommand>(),
                It.IsAny<Func<TestCommand, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestCommand cmd, Func<TestCommand, CancellationToken, Task<Result<string>>> next, CancellationToken ct) =>
            {
                var task = next(cmd, ct);
                task.Wait();
                return Result<string>.Success(task.Result.Value + " [Behavior Applied]");
            });

        var dispatcher = new CommandDispatcher(serviceProvider, new List<ICommandBehavior> { behaviorMock.Object });
        var command = new TestCommand { Value = "test" };

        // Act
        var result = await dispatcher.DispatchAsync<TestCommand, string>(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Handled: test [Behavior Applied]", result.Value);
        behaviorMock.Verify(b => b.HandleAsync<TestCommand, string>(
            It.IsAny<TestCommand>(),
            It.IsAny<Func<TestCommand, CancellationToken, Task<Result<string>>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleBehaviors_ShouldExecuteInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<TestCommand, string>, TestCommandHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var behavior1Mock = new Mock<ICommandBehavior>();
        behavior1Mock
            .Setup(b => b.HandleAsync<TestCommand, string>(
                It.IsAny<TestCommand>(),
                It.IsAny<Func<TestCommand, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestCommand cmd, Func<TestCommand, CancellationToken, Task<Result<string>>> next, CancellationToken ct) =>
            {
                var task = next(cmd, ct);
                task.Wait();
                return Result<string>.Success(task.Result.Value + " [B1]");
            });

        var behavior2Mock = new Mock<ICommandBehavior>();
        behavior2Mock
            .Setup(b => b.HandleAsync<TestCommand, string>(
                It.IsAny<TestCommand>(),
                It.IsAny<Func<TestCommand, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestCommand cmd, Func<TestCommand, CancellationToken, Task<Result<string>>> next, CancellationToken ct) =>
            {
                var task = next(cmd, ct);
                task.Wait();
                return Result<string>.Success(task.Result.Value + " [B2]");
            });

        var dispatcher = new CommandDispatcher(
            serviceProvider,
            new List<ICommandBehavior> { behavior1Mock.Object, behavior2Mock.Object });
        var command = new TestCommand { Value = "test" };

        // Act
        var result = await dispatcher.DispatchAsync<TestCommand, string>(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Handled: test [B2] [B1]", result.Value);
    }

    [Fact]
    public async Task DispatchAsync_CommandWithoutResult_WithRegisteredHandler_ShouldExecuteHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<TestCommandWithoutResult>, TestCommandWithoutResultHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new CommandDispatcher(serviceProvider, new List<ICommandBehavior>());
        var command = new TestCommandWithoutResult { Value = "test" };

        // Act
        var result = await dispatcher.DispatchAsync<TestCommandWithoutResult>(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DispatchAsync_WithCancellationToken_ShouldPassToHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var handlerMock = new Mock<ICommandHandler<TestCommand, string>>();
        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("result"));

        services.AddScoped<ICommandHandler<TestCommand, string>>(_ => handlerMock.Object);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new CommandDispatcher(serviceProvider, new List<ICommandBehavior>());
        var command = new TestCommand { Value = "test" };
        var cancellationToken = new CancellationToken();

        // Act
        await dispatcher.DispatchAsync<TestCommand, string>(command, cancellationToken);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(command, cancellationToken), Times.Once);
    }
}