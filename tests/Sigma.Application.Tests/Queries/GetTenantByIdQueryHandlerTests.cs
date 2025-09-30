using Moq;
using Sigma.Application.Queries;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Xunit;

namespace Sigma.Application.Tests.Queries;

public class GetTenantByIdQueryHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepository;
    private readonly GetTenantByIdQueryHandler _handler;

    public GetTenantByIdQueryHandlerTests()
    {
        _tenantRepository = new Mock<ITenantRepository>();
        _handler = new GetTenantByIdQueryHandler(_tenantRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithExistingTenant_ShouldReturnTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        var query = new GetTenantByIdQuery(tenantId);

        _tenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tenant, result.Value);
        _tenantRepository.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentTenant_ShouldReturnNotFoundError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetTenantByIdQuery(tenantId);

        _tenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error?.Code);
        Assert.Contains(tenantId.ToString(), result.Error?.Message);
        _tenantRepository.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetTenantByIdQuery(tenantId);
        var cancellationToken = new CancellationToken(true);

        _tenantRepository.Setup(x => x.GetByIdAsync(tenantId, cancellationToken))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        _tenantRepository.Verify(x => x.GetByIdAsync(tenantId, cancellationToken), Times.Once);
    }
}