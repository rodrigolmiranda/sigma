using Sigma.Domain.Entities;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.Domain.Tests.Entities;

public class TenantShould
{
    [Fact]
    public void BeCreatedWithValidData()
    {
        // Arrange
        var name = "Test Tenant";
        var slug = "test-tenant";
        var planType = "professional";
        var retentionDays = 90;

        // Act
        var tenant = new Tenant(name, slug, planType, retentionDays);

        // Assert
        Assert.NotNull(tenant);
        Assert.Equal(name, tenant.Name);
        Assert.Equal(slug, tenant.Slug);
        Assert.Equal(planType, tenant.PlanType);
        Assert.Equal(retentionDays, tenant.RetentionDays);
        Assert.True(tenant.IsActive);
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.NotEqual(default(DateTime), tenant.CreatedAtUtc);
        Assert.Null(tenant.UpdatedAtUtc);
    }

    [Fact]
    public void ThrowWhenCreatedWithNullName()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Tenant(null!, "slug", "free", 30));
    }

    [Fact]
    public void ThrowWhenCreatedWithNullSlug()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Tenant("name", null!, "free", 30));
    }

    [Fact]
    public void UseDefaultsWhenCreatedWithNullPlanType()
    {
        // Arrange & Act
        var tenant = new Tenant("Test", "test", null!, 30);

        // Assert
        Assert.Equal("free", tenant.PlanType);
    }

    [Fact]
    public void UseDefaultRetentionWhenInvalid()
    {
        // Arrange & Act
        var tenant = new Tenant("Test", "test", "free", -1);

        // Assert
        Assert.Equal(30, tenant.RetentionDays);
    }

    [Fact]
    public void UpdatePlanSuccessfully()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);
        // Act
        tenant.UpdatePlan("professional", 180);

        // Assert
        Assert.Equal("professional", tenant.PlanType);
        Assert.Equal(180, tenant.RetentionDays);
    }

    [Fact]
    public void ThrowWhenUpdatingWithEmptyPlanType()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tenant.UpdatePlan("", 30));
    }

    [Fact]
    public void ThrowWhenUpdatingWithInvalidRetentionDays()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tenant.UpdatePlan("professional", 0));
    }

    [Fact]
    public void DeactivateSuccessfully()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);
        Assert.True(tenant.IsActive);

        // Act
        tenant.Deactivate();

        // Assert
        Assert.False(tenant.IsActive);
    }

    [Fact]
    public void ActivateSuccessfully()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);
        tenant.Deactivate();
        Assert.False(tenant.IsActive);

        // Act
        tenant.Activate();

        // Assert
        Assert.True(tenant.IsActive);
    }

    [Fact]
    public void AddWorkspaceSuccessfully()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);
        var workspaceName = "Test Workspace";
        var platform = Platform.Slack;

        // Act
        var workspace = tenant.AddWorkspace(workspaceName, platform);

        // Assert
        Assert.NotNull(workspace);
        Assert.Equal(workspaceName, workspace.Name);
        Assert.Equal(platform, workspace.Platform);
        Assert.Equal(tenant.Id, workspace.TenantId);
        Assert.Single(tenant.Workspaces);
        Assert.Contains(workspace, tenant.Workspaces);
    }

    [Fact]
    public void AddMultipleWorkspaces_ShouldAddAll()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "enterprise", 365);

        // Act
        var workspace1 = tenant.AddWorkspace("Slack Workspace", Platform.Slack);
        var workspace2 = tenant.AddWorkspace("Discord Server", Platform.Discord);
        var workspace3 = tenant.AddWorkspace("WhatsApp Group", Platform.WhatsApp);

        // Assert
        Assert.Equal(3, tenant.Workspaces.Count);
        Assert.Contains(workspace1, tenant.Workspaces);
        Assert.Contains(workspace2, tenant.Workspaces);
        Assert.Contains(workspace3, tenant.Workspaces);
    }

    [Fact]
    public void UpdatePlan_MultipleTimes_ShouldUpdateCorrectly()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);

        // Act
        tenant.UpdatePlan("starter", 60);
        Assert.Equal("starter", tenant.PlanType);
        Assert.Equal(60, tenant.RetentionDays);

        tenant.UpdatePlan("professional", 180);
        Assert.Equal("professional", tenant.PlanType);
        Assert.Equal(180, tenant.RetentionDays);

        tenant.UpdatePlan("enterprise", 365);
        Assert.Equal("enterprise", tenant.PlanType);
        Assert.Equal(365, tenant.RetentionDays);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldRemainInactive()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);
        tenant.Deactivate();
        Assert.False(tenant.IsActive);
        var firstDeactivationTime = tenant.UpdatedAtUtc;

        // Act
        System.Threading.Thread.Sleep(10);
        tenant.Deactivate();

        // Assert
        Assert.False(tenant.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);
        Assert.True(tenant.IsActive);
        // Act
        tenant.Activate();

        // Assert
        Assert.True(tenant.IsActive);
    }

    [Theory]
    [InlineData("", 30)]
    [InlineData(" ", 30)]
    [InlineData("   ", 30)]
    [InlineData("\t", 30)]
    [InlineData("\n", 30)]
    public void UpdatePlan_WithWhitespacePlanType_ShouldThrowArgumentException(string planType, int retentionDays)
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => tenant.UpdatePlan(planType, retentionDays));
        Assert.Contains("Plan type cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-1)]
    [InlineData(0)]
    public void UpdatePlan_WithInvalidRetentionDays_ShouldThrowArgumentException(int retentionDays)
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => tenant.UpdatePlan("professional", retentionDays));
        Assert.Contains("Retention days must be positive", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyStrings_ShouldCreateSuccessfully()
    {
        // Act
        var tenant1 = new Tenant("", "test", "free", 30);
        var tenant2 = new Tenant("test", "", "free", 30);

        // Assert
        Assert.Equal("", tenant1.Name);
        Assert.Equal("test", tenant1.Slug);
        Assert.Equal("test", tenant2.Name);
        Assert.Equal("", tenant2.Slug);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(365)]
    [InlineData(730)]
    [InlineData(int.MaxValue)]
    public void Constructor_WithVariousRetentionDays_ShouldSetCorrectly(int retentionDays)
    {
        // Act
        var tenant = new Tenant("Test", "test", "enterprise", retentionDays);

        // Assert
        Assert.Equal(retentionDays, tenant.RetentionDays);
    }

    [Fact]
    public void Constructor_WithZeroRetentionDays_ShouldDefaultTo30()
    {
        // Act
        var tenant = new Tenant("Test", "test", "free", 0);

        // Assert
        Assert.Equal(30, tenant.RetentionDays);
    }

    [Fact]
    public void Constructor_WithNegativeRetentionDays_ShouldDefaultTo30()
    {
        // Act
        var tenant = new Tenant("Test", "test", "free", -100);

        // Assert
        Assert.Equal(30, tenant.RetentionDays);
    }

    [Fact]
    public void ExternalId_ShouldBeNullByDefault()
    {
        // Act
        var tenant = new Tenant("Test", "test", "free", 30);

        // Assert
        Assert.Null(tenant.ExternalId);
    }

    [Fact]
    public void Workspaces_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);
        tenant.AddWorkspace("Workspace 1", Platform.Slack);
        tenant.AddWorkspace("Workspace 2", Platform.Discord);

        // Act
        var workspaces = tenant.Workspaces;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<Workspace>>(workspaces);
        Assert.Equal(2, workspaces.Count);
    }

    [Fact]
    public void Entity_ShouldHaveUniqueId()
    {
        // Act
        var tenant1 = new Tenant("Test 1", "test1", "free", 30);
        var tenant2 = new Tenant("Test 2", "test2", "free", 30);

        // Assert
        Assert.NotEqual(tenant1.Id, tenant2.Id);
        Assert.NotEqual(Guid.Empty, tenant1.Id);
        Assert.NotEqual(Guid.Empty, tenant2.Id);
    }

    [Fact]
    public void Entity_ShouldTrackCreationAndUpdateTimes()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddMilliseconds(-100);

        // Act
        var tenant = new Tenant("Test", "test", "free", 30);
        var afterCreation = DateTime.UtcNow.AddMilliseconds(100);

        // Assert
        Assert.True(tenant.CreatedAtUtc >= beforeCreation);
        Assert.True(tenant.CreatedAtUtc <= afterCreation);
        Assert.Null(tenant.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("free")]
    [InlineData("starter")]
    [InlineData("professional")]
    [InlineData("enterprise")]
    public void Constructor_WithVariousPlanTypes_ShouldSetCorrectly(string planType)
    {
        // Act
        var tenant = new Tenant("Test", "test", planType, 30);

        // Assert
        Assert.Equal(planType.ToLowerInvariant(), tenant.PlanType);
    }

    [Fact]
    public void Constructor_WithLongStrings_ShouldCreateSuccessfully()
    {
        // Arrange
        var longName = new string('a', 1000);
        var longSlug = new string('b', 1000);

        // Act
        var tenant = new Tenant(longName, longSlug, "enterprise", 30);

        // Assert
        Assert.Equal(longName, tenant.Name);
        Assert.Equal(longSlug, tenant.Slug);
        Assert.Equal("enterprise", tenant.PlanType);
    }

    [Fact]
    public void UpdatePlan_ShouldUpdateProperties()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);

        // Act
        tenant.UpdatePlan("professional", 90);

        // Assert
        Assert.Equal("professional", tenant.PlanType);
        Assert.Equal(90, tenant.RetentionDays);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);

        // Act
        tenant.Deactivate();

        // Assert
        Assert.False(tenant.IsActive);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        // Arrange
        var tenant = new Tenant("Test", "test", "free", 30);
        tenant.Deactivate();

        // Act
        tenant.Activate();

        // Assert
        Assert.True(tenant.IsActive);
    }
}