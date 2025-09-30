using Moq;
using Sigma.API.GraphQL;
using Sigma.Application.Commands;
using Sigma.Application.Contracts;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.API.Tests.GraphQL;

public class MutationTests
{
    private readonly Mutation _mutation;
    private readonly Mock<ICommandDispatcher> _commandDispatcher;
    private readonly Mock<ITenantRepository> _tenantRepository;
    private readonly Mock<IWorkspaceRepository> _workspaceRepository;
    private readonly Mock<IChannelRepository> _channelRepository;
    private readonly Mock<IMessageRepository> _messageRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;

    public MutationTests()
    {
        _mutation = new Mutation();
        _commandDispatcher = new Mock<ICommandDispatcher>();
        _tenantRepository = new Mock<ITenantRepository>();
        _workspaceRepository = new Mock<IWorkspaceRepository>();
        _channelRepository = new Mock<IChannelRepository>();
        _messageRepository = new Mock<IMessageRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
    }

    [Fact]
    public async Task CreateTenant_WithValidInput_ShouldReturnSuccessPayload()
    {
        // Arrange
        var input = new CreateTenantInput(
            "Test Tenant",
            $"test-tenant-{Guid.NewGuid():N}",
            "professional",
            90);

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant(input.Name, input.Slug, input.PlanType ?? "free", input.RetentionDays ?? 30);

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateTenantCommand, Guid>(
                It.IsAny<CreateTenantCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(tenantId));

        _tenantRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _mutation.CreateTenant(
            input,
            _commandDispatcher.Object,
            _tenantRepository.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Tenant);
        Assert.Equal(input.Name, result.Tenant.Name);
        Assert.Equal(input.Slug, result.Tenant.Slug);
        Assert.Null(result.Errors);

        _commandDispatcher.Verify(d => d.DispatchAsync<CreateTenantCommand, Guid>(
            It.Is<CreateTenantCommand>(cmd =>
                cmd.Name == input.Name &&
                cmd.Slug == input.Slug &&
                cmd.PlanType == input.PlanType &&
                cmd.RetentionDays == input.RetentionDays),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTenant_WithDefaultValues_ShouldUseDefaults()
    {
        // Arrange
        var input = new CreateTenantInput(
            "Test Tenant",
            $"test-tenant-{Guid.NewGuid():N}",
            null,
            null);

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant(input.Name, input.Slug, "free", 30);

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateTenantCommand, Guid>(
                It.IsAny<CreateTenantCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(tenantId));

        _tenantRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _mutation.CreateTenant(
            input,
            _commandDispatcher.Object,
            _tenantRepository.Object,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        _commandDispatcher.Verify(d => d.DispatchAsync<CreateTenantCommand, Guid>(
            It.Is<CreateTenantCommand>(cmd =>
                cmd.PlanType == "free" &&
                cmd.RetentionDays == 30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTenant_WithFailedCommand_ShouldReturnErrorPayload()
    {
        // Arrange
        var input = new CreateTenantInput(
            "Test Tenant",
            "existing-slug",
            "free",
            30);

        var error = new Error("DUPLICATE_SLUG", "Slug already exists");

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateTenantCommand, Guid>(
                It.IsAny<CreateTenantCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure(error));

        // Act
        var result = await _mutation.CreateTenant(
            input,
            _commandDispatcher.Object,
            _tenantRepository.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Null(result.Tenant);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Equal("Slug already exists", result.Errors.First().Message);
        Assert.Equal("DUPLICATE_SLUG", result.Errors.First().Code);

        _tenantRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTenantPlan_WithExistingTenant_ShouldUpdateSuccessfully()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var input = new UpdateTenantPlanInput(
            tenantId,
            "professional",
            180);

        var tenant = new Tenant("Test Tenant", $"test-tenant-{Guid.NewGuid():N}", "free", 30);

        _tenantRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _tenantRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _mutation.UpdateTenantPlan(
            input,
            _tenantRepository.Object,
            _unitOfWork.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Tenant);
        Assert.Equal("professional", result.Tenant.PlanType);
        Assert.Equal(180, result.Tenant.RetentionDays);
        Assert.Null(result.Errors);

        _tenantRepository.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTenantPlan_WithNonExistingTenant_ShouldReturnError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var input = new UpdateTenantPlanInput(
            tenantId,
            "professional",
            180);

        _tenantRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _mutation.UpdateTenantPlan(
            input,
            _tenantRepository.Object,
            _unitOfWork.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Null(result.Tenant);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Equal("Tenant not found", result.Errors.First().Message);
        Assert.Equal("NOT_FOUND", result.Errors.First().Code);

        _tenantRepository.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateWorkspace_WithValidInput_ShouldReturnSuccessPayload()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var input = new CreateWorkspaceInput(
            tenantId,
            "Test Workspace",
            Platform.Slack.ToString(),
            "slack-12345");

        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace(
            tenantId,
            input.Name,
            input.Platform);

        if (!string.IsNullOrEmpty(input.ExternalId))
        {
            workspace.UpdateExternalId(input.ExternalId);
        }

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateWorkspaceCommand, Guid>(
                It.IsAny<CreateWorkspaceCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(workspaceId));

        _workspaceRepository.Setup(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        // Act
        var result = await _mutation.CreateWorkspace(
            input,
            _commandDispatcher.Object,
            _workspaceRepository.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Workspace);
        Assert.Equal(input.Name, result.Workspace.Name);
        Assert.Equal(input.Platform, result.Workspace.Platform);
        Assert.Equal(input.ExternalId, result.Workspace.ExternalId);
        Assert.Null(result.Errors);

        _commandDispatcher.Verify(d => d.DispatchAsync<CreateWorkspaceCommand, Guid>(
            It.Is<CreateWorkspaceCommand>(cmd =>
                cmd.TenantId == input.TenantId &&
                cmd.Name == input.Name &&
                cmd.Platform == input.Platform &&
                cmd.ExternalId == input.ExternalId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWorkspace_WithFailedCommand_ShouldReturnErrorPayload()
    {
        // Arrange
        var input = new CreateWorkspaceInput(
            Guid.NewGuid(),
            "Test Workspace",
            Platform.Discord.ToString(),
            "discord-12345");

        var error = new Error("DUPLICATE_EXTERNAL_ID", "Workspace with this external ID already exists");

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateWorkspaceCommand, Guid>(
                It.IsAny<CreateWorkspaceCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure(error));

        // Act
        var result = await _mutation.CreateWorkspace(
            input,
            _commandDispatcher.Object,
            _workspaceRepository.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Null(result.Workspace);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Equal("Workspace with this external ID already exists", result.Errors.First().Message);
        Assert.Equal("DUPLICATE_EXTERNAL_ID", result.Errors.First().Code);

        _workspaceRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateChannel_WithValidInput_ShouldReturnSuccessPayload()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var input = new CreateChannelInput(
            tenantId,
            workspaceId,
            "general",
            "C12345");

        var channelId = Guid.NewGuid();
        var workspace = new Workspace(tenantId, "Test Workspace", "Slack");
        var channel = workspace.AddChannel(input.Name, input.ExternalId);

        _workspaceRepository.Setup(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        _channelRepository.Setup(r => r.AddAsync(It.IsAny<Channel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _mutation.CreateChannel(
            input,
            _workspaceRepository.Object,
            _channelRepository.Object,
            _unitOfWork.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Channel);
        Assert.Equal(input.Name, result.Channel.Name);
        Assert.Equal(input.ExternalId, result.Channel.ExternalId);
        Assert.Null(result.Errors);
    }

    [Theory]
    [InlineData("Slack")]
    [InlineData("Discord")]
    [InlineData("WhatsApp")]
    [InlineData("Telegram")]
    public async Task CreateWorkspace_WithVariousPlatforms_ShouldHandleCorrectly(string platform)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var input = new CreateWorkspaceInput(
            tenantId,
            $"{platform} Workspace",
            platform,
            $"{platform.ToLower()}-12345");

        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace(
            tenantId,
            input.Name,
            platform);

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateWorkspaceCommand, Guid>(
                It.IsAny<CreateWorkspaceCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(workspaceId));

        _workspaceRepository.Setup(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        // Act
        var result = await _mutation.CreateWorkspace(
            input,
            _commandDispatcher.Object,
            _workspaceRepository.Object,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Workspace);
        Assert.Equal(platform, result.Workspace.Platform);
    }

    [Fact]
    public async Task CreateTenant_WithCancellation_ShouldPropagateCancellation()
    {
        // Arrange
        var input = new CreateTenantInput(
            "Test",
            "test",
            null,
            null);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateTenantCommand, Guid>(
                It.IsAny<CreateTenantCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _mutation.CreateTenant(input, _commandDispatcher.Object, _tenantRepository.Object, cts.Token));
    }

    [Fact]
    public async Task UpdateTenantPlan_WithUnitOfWorkFailure_ShouldReturnError()
    {
        // Arrange
        var input = new UpdateTenantPlanInput(
            Guid.NewGuid(),
            "enterprise",
            365);

        var tenant = new Tenant("Test Tenant", $"test-tenant-{Guid.NewGuid():N}", "free", 30);

        _tenantRepository.Setup(r => r.GetByIdAsync(input.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _tenantRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _mutation.UpdateTenantPlan(input, _tenantRepository.Object, _unitOfWork.Object, CancellationToken.None));
    }

    [Fact]
    public async Task CreateWorkspace_WithNullExternalId_ShouldSucceed()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var input = new CreateWorkspaceInput(
            tenantId,
            "Test Workspace",
            "Telegram",
            null);

        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace(tenantId, input.Name, input.Platform);

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateWorkspaceCommand, Guid>(
                It.IsAny<CreateWorkspaceCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(workspaceId));

        _workspaceRepository.Setup(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        // Act
        var result = await _mutation.CreateWorkspace(
            input,
            _commandDispatcher.Object,
            _workspaceRepository.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Workspace);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task CreateChannel_WithWorkspaceNotFound_ShouldReturnError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var input = new CreateChannelInput(
            tenantId,
            workspaceId,
            "general",
            "CH123");

        _workspaceRepository.Setup(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        // Act
        var result = await _mutation.CreateChannel(
            input,
            _workspaceRepository.Object,
            _channelRepository.Object,
            _unitOfWork.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "NOT_FOUND");
        Assert.Contains(result.Errors, e => e.Message.Contains("Workspace not found"));
    }

    [Fact]
    public async Task CreateChannel_WithUnitOfWorkFailure_ShouldThrowException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var input = new CreateChannelInput(
            tenantId,
            workspaceId,
            "general",
            "CH123");

        var workspace = new Workspace(tenantId, "Test Workspace", "Slack");

        _workspaceRepository.Setup(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        _channelRepository.Setup(r => r.AddAsync(It.IsAny<Channel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Save failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _mutation.CreateChannel(
                input,
                _workspaceRepository.Object,
                _channelRepository.Object,
                _unitOfWork.Object,
                CancellationToken.None));
    }

    [Theory]
    [InlineData("free", 30)]
    [InlineData("professional", 90)]
    [InlineData("enterprise", 365)]
    public async Task UpdateTenantPlan_WithVariousPlans_ShouldUpdateCorrectly(string planType, int retentionDays)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var input = new UpdateTenantPlanInput(tenantId, planType, retentionDays);
        var tenant = new Tenant("Test Tenant", $"test-tenant-{Guid.NewGuid():N}", "free", 30);

        _tenantRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _tenantRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _mutation.UpdateTenantPlan(
            input,
            _tenantRepository.Object,
            _unitOfWork.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Tenant);
        Assert.Null(result.Errors);
        _tenantRepository.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTenant_WithRepositoryGetByIdFailure_ShouldStillReturnSuccess()
    {
        // Arrange
        var input = new CreateTenantInput(
            "Test Tenant",
            $"test-tenant-{Guid.NewGuid():N}",
            "professional",
            90);

        var tenantId = Guid.NewGuid();

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateTenantCommand, Guid>(
                It.IsAny<CreateTenantCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(tenantId));

        _tenantRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _mutation.CreateTenant(
            input,
            _commandDispatcher.Object,
            _tenantRepository.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Null(result.Tenant);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task CreateWorkspace_WithRepositoryGetByIdFailure_ShouldStillReturnSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var input = new CreateWorkspaceInput(
            tenantId,
            "Test Workspace",
            "Discord",
            "WS123");

        var workspaceId = Guid.NewGuid();

        _commandDispatcher.Setup(d => d.DispatchAsync<CreateWorkspaceCommand, Guid>(
                It.IsAny<CreateWorkspaceCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(workspaceId));

        _workspaceRepository.Setup(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        // Act
        var result = await _mutation.CreateWorkspace(
            input,
            _commandDispatcher.Object,
            _workspaceRepository.Object,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Null(result.Workspace);
        Assert.Null(result.Errors);
    }
}