using Microsoft.Extensions.Logging;
using Moq;
using Sigma.Application.Behaviors;
using Sigma.Application.Contracts;
using Sigma.Domain.Contracts;
using Xunit;

namespace Sigma.Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior>> _logger;
    private readonly Mock<ICorrelationContext> _correlationContext;
    private readonly LoggingBehavior _behavior;

    public LoggingBehaviorTests()
    {
        _logger = new Mock<ILogger<LoggingBehavior>>();
        _correlationContext = new Mock<ICorrelationContext>();
        _correlationContext.Setup(x => x.CorrelationId).Returns("test-correlation-id");
        _behavior = new LoggingBehavior(_logger.Object, _correlationContext.Object);
    }

    public class TestCommand : ICommand<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestQuery : IQuery<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    [Fact]
    public async Task HandleAsync_Command_WithSuccessfulExecution_ShouldLogInformation()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result<string>.Success("success");

        // Act
        var result = await _behavior.HandleAsync<TestCommand, string>(
            command,
            (cmd, ct) => Task.FromResult(expectedResult));

        // Assert
        Assert.True(result.IsSuccess);
        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing command TestCommand")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("executed successfully")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Command_WithFailedExecution_ShouldLogWarning()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result<string>.Failure(Error.Validation("Test error"));

        // Act
        var result = await _behavior.HandleAsync<TestCommand, string>(
            command,
            (cmd, ct) => Task.FromResult(expectedResult));

        // Assert
        Assert.False(result.IsSuccess);
        _logger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed with error")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Command_WithException_ShouldLogError()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _behavior.HandleAsync<TestCommand, string>(
                command,
                (cmd, ct) => throw expectedException));

        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("threw exception")),
            expectedException,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Query_WithSuccessfulExecution_ShouldLogInformation()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var expectedResult = Result<string>.Success("success");

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            (q, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing query TestQuery")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("executed successfully")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Query_WithFailedExecution_ShouldLogWarning()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var expectedResult = Result<string>.Failure(Error.NotFound("Not found"));

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            (q, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        _logger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed with error")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Query_WithException_ShouldLogError()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
                query,
                (q, ct) => throw expectedException,
                TestContext.Current.CancellationToken));

        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("threw exception")),
            expectedException,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutResult_WithSuccessfulExecution_ShouldLogInformation()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result.Success();

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            (cmd, ct) => Task.FromResult(expectedResult));

        // Assert
        Assert.True(result.IsSuccess);
        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing command TestCommand")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("executed successfully")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutResult_WithFailure_ShouldLogWarning()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedError = new Error("TEST_ERROR", "Test error message");
        var expectedResult = Result.Failure(expectedError);

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            (cmd, ct) => Task.FromResult(expectedResult));

        // Assert
        Assert.False(result.IsSuccess);
        _logger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed with error TEST_ERROR")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutResult_WithException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _behavior.HandleAsync<TestCommand>(
                command,
                (cmd, ct) => throw exception));

        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("threw exception")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Command_WithCancellationToken_ShouldPassTokenToNext()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result.Success();
        var cancellationToken = new CancellationToken();
        var tokenReceived = default(CancellationToken);

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            (cmd, ct) =>
            {
                tokenReceived = ct;
                return Task.FromResult(expectedResult);
            },
            cancellationToken);

        // Assert
        Assert.Equal(cancellationToken, tokenReceived);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_Query_WithCancellationToken_ShouldPassTokenToNext()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var expectedResult = Result<string>.Success("result");
        var cancellationToken = new CancellationToken();
        var tokenReceived = default(CancellationToken);

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            (qry, ct) =>
            {
                tokenReceived = ct;
                return Task.FromResult(expectedResult);
            },
            cancellationToken);

        // Assert
        Assert.Equal(cancellationToken, tokenReceived);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_CommandWithLongExecution_ShouldLogCorrectElapsedTime()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result.Success();

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            async (cmd, ct) =>
            {
                await Task.Delay(100); // Simulate long-running operation
                return expectedResult;
            });

        // Assert
        Assert.True(result.IsSuccess);
        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString()!.Contains("executed successfully") &&
                v.ToString()!.Contains("ms")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_QueryWithLongExecution_ShouldLogCorrectElapsedTime()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var expectedResult = Result<string>.Success("result");

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            async (qry, ct) =>
            {
                await Task.Delay(100); // Simulate long-running operation
                return expectedResult;
            });

        // Assert
        Assert.True(result.IsSuccess);
        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString()!.Contains("executed successfully") &&
                v.ToString()!.Contains("ms")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}