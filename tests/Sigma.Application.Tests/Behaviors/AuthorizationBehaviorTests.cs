using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Sigma.Application.Behaviors;
using Sigma.Application.Contracts;
using Sigma.Domain.Authorization;
using Xunit;

namespace Sigma.Application.Tests.Behaviors;

public class AuthorizationBehaviorTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly AuthorizationBehavior _behavior;

    public AuthorizationBehaviorTests()
    {
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _serviceProvider = new Mock<IServiceProvider>();
        _behavior = new AuthorizationBehavior(_httpContextAccessor.Object, _serviceProvider.Object);
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
    public async Task HandleAsync_Command_WithNoAuthorizationHandler_ShouldCallNext()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result<string>.Success("success");

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
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
    public async Task HandleAsync_Command_WithNoUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var authHandler = new Mock<IAuthorizationHandler<TestCommand>>();

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns((HttpContext?)null);

        // Act
        var result = await _behavior.HandleAsync<TestCommand, string>(
            command,
            (cmd, ct) => Task.FromResult(Result<string>.Success("success")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error?.Code);
        Assert.Equal("User is not authenticated", result.Error?.Message);
    }

    [Fact]
    public async Task HandleAsync_Command_WithUnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var authHandler = new Mock<IAuthorizationHandler<TestCommand>>();
        var httpContext = new DefaultHttpContext();
        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.IsAuthenticated).Returns(false);
        httpContext.User = new ClaimsPrincipal(identity.Object);

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = await _behavior.HandleAsync<TestCommand, string>(
            command,
            (cmd, ct) => Task.FromResult(Result<string>.Success("success")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error?.Code);
        Assert.Equal("User is not authenticated", result.Error?.Message);
    }

    [Fact]
    public async Task HandleAsync_Command_WithAuthorizedUser_ShouldCallNext()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result<string>.Success("success");
        var authHandler = new Mock<IAuthorizationHandler<TestCommand>>();
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        httpContext.User = new ClaimsPrincipal(identity);

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext);

        authHandler.Setup(x => x.AuthorizeAsync(command, httpContext.User))
            .ReturnsAsync(true);

        // Act
        var result = await _behavior.HandleAsync<TestCommand, string>(
            command,
            (cmd, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Value);
        authHandler.Verify(x => x.AuthorizeAsync(command, httpContext.User), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Command_WithUnauthorizedUser_ShouldReturnForbidden()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var authHandler = new Mock<IAuthorizationHandler<TestCommand>>();
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        httpContext.User = new ClaimsPrincipal(identity);

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext);

        authHandler.Setup(x => x.AuthorizeAsync(command, httpContext.User))
            .ReturnsAsync(false);

        // Act
        var result = await _behavior.HandleAsync<TestCommand, string>(
            command,
            (cmd, ct) => Task.FromResult(Result<string>.Success("success")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error?.Code);
        Assert.Equal("User is not authorized to perform this action", result.Error?.Message);
        authHandler.Verify(x => x.AuthorizeAsync(command, httpContext.User), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Query_WithNoAuthorizationHandler_ShouldCallNext()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var expectedResult = Result<string>.Success("success");

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestQuery>)))
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
    public async Task HandleAsync_Query_WithAuthorizedUser_ShouldCallNext()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var expectedResult = Result<string>.Success("success");
        var authHandler = new Mock<IAuthorizationHandler<TestQuery>>();
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        httpContext.User = new ClaimsPrincipal(identity);

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestQuery>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext);

        authHandler.Setup(x => x.AuthorizeAsync(query, httpContext.User))
            .ReturnsAsync(true);

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            (q, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Value);
        authHandler.Verify(x => x.AuthorizeAsync(query, httpContext.User), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutResult_WithAuthorizedUser_ShouldCallNext()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result.Success();
        var authHandler = new Mock<IAuthorizationHandler<TestCommand>>();
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        httpContext.User = new ClaimsPrincipal(identity);

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext);

        authHandler.Setup(x => x.AuthorizeAsync(command, httpContext.User))
            .ReturnsAsync(true);

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            (cmd, ct) => Task.FromResult(expectedResult),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        authHandler.Verify(x => x.AuthorizeAsync(command, httpContext.User), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutResult_WithUnauthorizedUser_ShouldReturnForbidden()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var authHandler = new Mock<IAuthorizationHandler<TestCommand>>();
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        httpContext.User = new ClaimsPrincipal(identity);

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext);

        authHandler.Setup(x => x.AuthorizeAsync(command, httpContext.User))
            .ReturnsAsync(false);

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            (cmd, ct) => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error?.Code);
    }

    [Fact]
    public async Task HandleAsync_Query_WithUnauthorizedUser_ShouldReturnForbidden()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var authHandler = new Mock<IAuthorizationHandler<TestQuery>>();
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        httpContext.User = new ClaimsPrincipal(identity);

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestQuery>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext);

        authHandler.Setup(x => x.AuthorizeAsync(query, httpContext.User))
            .ReturnsAsync(false);

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            (qry, ct) => Task.FromResult(Result<string>.Success("result")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error?.Code);
    }

    [Fact]
    public async Task HandleAsync_Query_WithNoUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var query = new TestQuery { Value = "test" };
        var authHandler = new Mock<IAuthorizationHandler<TestQuery>>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(); // No identity

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestQuery>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = await ((IQueryBehavior)_behavior).HandleAsync<TestQuery, string>(
            query,
            (qry, ct) => Task.FromResult(Result<string>.Success("result")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error?.Code);
    }

    [Fact]
    public async Task HandleAsync_Command_WithCancellationToken_ShouldPassTokenToNext()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var expectedResult = Result.Success();
        var cancellationToken = new CancellationToken();
        var tokenReceived = default(CancellationToken);

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
            .Returns(null);

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

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestQuery>)))
            .Returns(null);

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
    public async Task HandleAsync_Command_WithAuthHandlerThrowingException_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var authHandler = new Mock<IAuthorizationHandler<TestCommand>>();
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        httpContext.User = new ClaimsPrincipal(identity);

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext);

        authHandler.Setup(x => x.AuthorizeAsync(command, httpContext.User))
            .ThrowsAsync(new InvalidOperationException("Auth handler error"));

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            (cmd, ct) => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error?.Code);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutResult_WithNoHttpContext_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new TestCommand { Value = "test" };
        var authHandler = new Mock<IAuthorizationHandler<TestCommand>>();

        _serviceProvider.Setup(x => x.GetService(typeof(IAuthorizationHandler<TestCommand>)))
            .Returns(authHandler.Object);

        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns((HttpContext?)null);

        // Act
        var result = await _behavior.HandleAsync<TestCommand>(
            command,
            (cmd, ct) => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error?.Code);
    }
}