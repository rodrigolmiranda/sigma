using FluentValidation;
using FluentValidation.Results;
using Moq;
using Sigma.Application.Behaviors;
using Sigma.Application.Contracts;
using Xunit;

namespace Sigma.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly ValidationBehavior _behavior;

    public ValidationBehaviorTests()
    {
        _serviceProvider = new Mock<IServiceProvider>();
        _behavior = new ValidationBehavior(_serviceProvider.Object);
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
    public async Task HandleAsync_Command_WithNoValidator_ShouldCallNext()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result<string>.Success("success");

        _serviceProvider.Setup(x => x.GetService(typeof(IValidator<TestCommand>)))
            .Returns(null);

        // Act
        var result = await _behavior.HandleAsync<TestCommand, string>(
            command,
            (cmd, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task HandleAsync_Command_WithValidatorPassingValidation_ShouldCallNext()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result<string>.Success("success");
        var validator = new Mock<IValidator<TestCommand>>();

        validator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _serviceProvider.Setup(x => x.GetService(typeof(IValidator<TestCommand>)))
            .Returns(validator.Object);

        // Act
        var result = await _behavior.HandleAsync<TestCommand, string>(
            command,
            (cmd, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Value);
        validator.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Command_WithValidatorFailingValidation_ShouldReturnValidationError()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var validator = new Mock<IValidator<TestCommand>>();
        var validationFailure = new ValidationFailure("Value", "Value is required");

        validator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { validationFailure }));

        _serviceProvider.Setup(x => x.GetService(typeof(IValidator<TestCommand>)))
            .Returns(validator.Object);

        // Act
        var result = await _behavior.HandleAsync<TestCommand, string>(
            command,
            (cmd, ct) => Task.FromResult(Result<string>.Success("success")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error?.Code);
        Assert.Equal("Value is required", result.Error?.Message);
        validator.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Query_WithNoValidator_ShouldCallNext()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var expectedResult = Result<string>.Success("success");

        _serviceProvider.Setup(x => x.GetService(typeof(IValidator<TestQuery>)))
            .Returns(null);

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            (q, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task HandleAsync_Query_WithValidatorPassingValidation_ShouldCallNext()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var expectedResult = Result<string>.Success("success");
        var validator = new Mock<IValidator<TestQuery>>();

        validator.Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _serviceProvider.Setup(x => x.GetService(typeof(IValidator<TestQuery>)))
            .Returns(validator.Object);

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            (q, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Value);
        validator.Verify(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Query_WithValidatorFailingValidation_ShouldReturnValidationError()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var validator = new Mock<IValidator<TestQuery>>();
        var validationFailure = new ValidationFailure("Value", "Value is required");

        validator.Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { validationFailure }));

        _serviceProvider.Setup(x => x.GetService(typeof(IValidator<TestQuery>)))
            .Returns(validator.Object);

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            (q, ct) => Task.FromResult(Result<string>.Success("success")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error?.Code);
        Assert.Equal("Value is required", result.Error?.Message);
        validator.Verify(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutResult_WithNoValidator_ShouldCallNext()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result.Success();

        _serviceProvider.Setup(x => x.GetService(typeof(IValidator<TestCommand>)))
            .Returns(null);

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            (cmd, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutResult_WithValidatorFailingValidation_ShouldReturnValidationError()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var validator = new Mock<IValidator<TestCommand>>();
        var validationFailure = new ValidationFailure("Value", "Value is required");

        validator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { validationFailure }));

        _serviceProvider.Setup(x => x.GetService(typeof(IValidator<TestCommand>)))
            .Returns(validator.Object);

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            (cmd, ct) => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error?.Code);
        Assert.Equal("Value is required", result.Error?.Message);
        validator.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}