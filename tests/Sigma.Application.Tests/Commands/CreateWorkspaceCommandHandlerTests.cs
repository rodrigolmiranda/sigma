using Moq;
using Sigma.Application.Commands;
using Sigma.Application.Contracts;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Xunit;

namespace Sigma.Application.Tests.Commands;

public class CreateWorkspaceCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepository;
    private readonly Mock<IWorkspaceRepository> _workspaceRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly CreateWorkspaceCommandHandler _handler;

    public CreateWorkspaceCommandHandlerTests()
    {
        _tenantRepository = new Mock<ITenantRepository>();
        _workspaceRepository = new Mock<IWorkspaceRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateWorkspaceCommandHandler(_tenantRepository.Object, _workspaceRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldCreateWorkspace()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        var command = new CreateWorkspaceCommand(tenantId, "Test Workspace", "Slack", "T123456");

        _tenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentTenant_ShouldReturnNotFoundError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new CreateWorkspaceCommand(tenantId, "Test Workspace", "Slack", null);

        _tenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error?.Code);
        Assert.Contains("Tenant", result.Error?.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithExternalId_ShouldSetExternalId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        var command = new CreateWorkspaceCommand(tenantId, "Test Workspace", "Slack", "T123456");

        _tenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var workspace = tenant.Workspaces.FirstOrDefault();
        Assert.NotNull(workspace);
        Assert.Equal("T123456", workspace.ExternalId);
    }

    [Fact]
    public async Task HandleAsync_WithoutExternalId_ShouldCreateWorkspaceWithoutExternalId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        var command = new CreateWorkspaceCommand(tenantId, "Test Workspace", "Discord", null);

        _tenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var workspace = tenant.Workspaces.FirstOrDefault();
        Assert.NotNull(workspace);
        Assert.Null(workspace.ExternalId);
    }
}