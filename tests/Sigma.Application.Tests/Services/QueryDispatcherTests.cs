using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sigma.Application.Behaviors;
using Sigma.Application.Contracts;
using Sigma.Application.Services;
using Xunit;

namespace Sigma.Application.Tests.Services;

public class QueryDispatcherTests
{
    public class TestQuery : IQuery<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestQueryHandler : IQueryHandler<TestQuery, string>
    {
        public Task<Result<string>> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<string>.Success($"Query result: {query.Value}"));
        }
    }

    [Fact]
    public async Task DispatchAsync_WithRegisteredHandler_ShouldExecuteHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IQueryHandler<TestQuery, string>, TestQueryHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new QueryDispatcher(serviceProvider, new List<IQueryBehavior>());
        var query = new TestQuery { Value = "test" };

        // Act
        var result = await dispatcher.DispatchAsync<TestQuery, string>(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Query result: test", result.Value);
    }

    [Fact]
    public async Task DispatchAsync_WithoutRegisteredHandler_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new QueryDispatcher(serviceProvider, new List<IQueryBehavior>());
        var query = new TestQuery { Value = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync<TestQuery, string>(query, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DispatchAsync_WithBehavior_ShouldExecuteBehaviorPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IQueryHandler<TestQuery, string>, TestQueryHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var behaviorMock = new Mock<IQueryBehavior>();
        behaviorMock
            .Setup(b => b.HandleAsync<TestQuery, string>(
                It.IsAny<TestQuery>(),
                It.IsAny<Func<TestQuery, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<TestQuery, Func<TestQuery, CancellationToken, Task<Result<string>>>, CancellationToken>(
                async (query, next, ct) =>
                {
                    var result = await next(query, ct);
                    return Result<string>.Success(result.Value + " [Behavior Applied]");
                });

        var dispatcher = new QueryDispatcher(serviceProvider, new List<IQueryBehavior> { behaviorMock.Object });
        var query = new TestQuery { Value = "test" };

        // Act
        var result = await dispatcher.DispatchAsync<TestQuery, string>(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Query result: test [Behavior Applied]", result.Value);
        behaviorMock.Verify(b => b.HandleAsync<TestQuery, string>(
            It.IsAny<TestQuery>(),
            It.IsAny<Func<TestQuery, CancellationToken, Task<Result<string>>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleBehaviors_ShouldExecuteInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IQueryHandler<TestQuery, string>, TestQueryHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var behavior1Mock = new Mock<IQueryBehavior>();
        behavior1Mock
            .Setup(b => b.HandleAsync<TestQuery, string>(
                It.IsAny<TestQuery>(),
                It.IsAny<Func<TestQuery, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<TestQuery, Func<TestQuery, CancellationToken, Task<Result<string>>>, CancellationToken>(
                async (query, next, ct) =>
                {
                    var result = await next(query, ct);
                    return Result<string>.Success(result.Value + " [B1]");
                });

        var behavior2Mock = new Mock<IQueryBehavior>();
        behavior2Mock
            .Setup(b => b.HandleAsync<TestQuery, string>(
                It.IsAny<TestQuery>(),
                It.IsAny<Func<TestQuery, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<TestQuery, Func<TestQuery, CancellationToken, Task<Result<string>>>, CancellationToken>(
                async (query, next, ct) =>
                {
                    var result = await next(query, ct);
                    return Result<string>.Success(result.Value + " [B2]");
                });

        var dispatcher = new QueryDispatcher(
            serviceProvider,
            new List<IQueryBehavior> { behavior1Mock.Object, behavior2Mock.Object });
        var query = new TestQuery { Value = "test" };

        // Act
        var result = await dispatcher.DispatchAsync<TestQuery, string>(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Query result: test [B2] [B1]", result.Value);
    }

    [Fact]
    public async Task DispatchAsync_WithCancellationToken_ShouldPassToHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var handlerMock = new Mock<IQueryHandler<TestQuery, string>>();
        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("result"));

        services.AddScoped<IQueryHandler<TestQuery, string>>(_ => handlerMock.Object);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new QueryDispatcher(serviceProvider, new List<IQueryBehavior>());
        var query = new TestQuery { Value = "test" };
        var cancellationToken = new CancellationToken();

        // Act
        await dispatcher.DispatchAsync<TestQuery, string>(query, cancellationToken);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(query, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithBehaviorReturningError_ShouldShortCircuit()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IQueryHandler<TestQuery, string>, TestQueryHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var behaviorMock = new Mock<IQueryBehavior>();
        behaviorMock
            .Setup(b => b.HandleAsync<TestQuery, string>(
                It.IsAny<TestQuery>(),
                It.IsAny<Func<TestQuery, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure(Error.Validation("Query validation failed")));

        var dispatcher = new QueryDispatcher(serviceProvider, new List<IQueryBehavior> { behaviorMock.Object });
        var query = new TestQuery { Value = "test" };

        // Act
        var result = await dispatcher.DispatchAsync<TestQuery, string>(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error?.Code);
        Assert.Equal("Query validation failed", result.Error?.Message);
    }
}