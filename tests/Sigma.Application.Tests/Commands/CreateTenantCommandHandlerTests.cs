using Microsoft.Extensions.Logging;
using Moq;
using Sigma.Application.Commands;
using Sigma.Application.Contracts;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Xunit;

namespace Sigma.Application.Tests.Commands;

public class CreateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<CreateTenantCommandHandler>> _logger;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _tenantRepository = new Mock<ITenantRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger<CreateTenantCommandHandler>>();
        _handler = new CreateTenantCommandHandler(_tenantRepository.Object, _unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldCreateTenant()
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "test-tenant", "professional", 90);
        _tenantRepository.Setup(x => x.GetBySlugAsync("test-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _tenantRepository.Verify(x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithExistingSlug_ShouldReturnConflictError()
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "existing-slug", "free", 30);
        var existingTenant = new Tenant("Existing", "existing-slug", "free", 30);
        _tenantRepository.Setup(x => x.GetBySlugAsync("existing-slug", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);

        // Act
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.Error?.Code);
        Assert.Contains("already exists", result.Error?.Message);
        _tenantRepository.Verify(x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNullPlanType_ShouldUseDefault()
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "test-tenant", null, 30);
        _tenantRepository.Setup(x => x.GetBySlugAsync("test-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        Tenant? capturedTenant = null;
        _tenantRepository.Setup(x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Callback<Tenant, CancellationToken>((t, _) => capturedTenant = t);

        // Act
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedTenant);
        Assert.Equal("free", capturedTenant.PlanType);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidRetentionDays_ShouldUseDefault()
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "test-tenant", "free", -1);
        _tenantRepository.Setup(x => x.GetBySlugAsync("test-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        Tenant? capturedTenant = null;
        _tenantRepository.Setup(x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Callback<Tenant, CancellationToken>((t, _) => capturedTenant = t);

        // Act
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedTenant);
        Assert.Equal(30, capturedTenant.RetentionDays);
    }
}