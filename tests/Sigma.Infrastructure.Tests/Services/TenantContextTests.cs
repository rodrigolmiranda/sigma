using System;
using System.Collections.Generic;
using System.Linq;
using Sigma.Infrastructure.Services;
using Xunit;

namespace Sigma.Infrastructure.Tests.Services;

public class TenantContextTests
{
    [Fact]
    public void Constructor_Default_CreatesAnonymousContext()
    {
        // Act
        var context = new TenantContext();

        // Assert
        Assert.Equal(Guid.Empty, context.TenantId);
        Assert.Equal(string.Empty, context.TenantSlug);
        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.NotNull(context.Roles);
        Assert.Empty(context.Roles);
    }

    [Fact]
    public void Constructor_WithAllParameters_CreatesCorrectContext()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantSlug = "test-tenant";
        var isAuthenticated = true;
        var userId = "user123";
        var roles = new[] { "admin", "user" };

        // Act
        var context = new TenantContext(tenantId, tenantSlug, isAuthenticated, userId, roles);

        // Assert
        Assert.Equal(tenantId, context.TenantId);
        Assert.Equal(tenantSlug, context.TenantSlug);
        Assert.True(context.IsAuthenticated);
        Assert.Equal(userId, context.UserId);
        Assert.Equal(2, context.Roles.Count);
        Assert.Contains("admin", context.Roles);
        Assert.Contains("user", context.Roles);
    }

    [Fact]
    public void Constructor_WithNullSlug_SetsEmptyString()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var context = new TenantContext(tenantId, null!, true, "user123");

        // Assert
        Assert.Equal(string.Empty, context.TenantSlug);
    }

    [Fact]
    public void Constructor_WithNullRoles_CreatesEmptyRolesList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var context = new TenantContext(tenantId, "tenant", true, "user123", null);

        // Assert
        Assert.NotNull(context.Roles);
        Assert.Empty(context.Roles);
    }

    [Fact]
    public void Constructor_WithNullUserId_AllowsNullUserId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var context = new TenantContext(tenantId, "tenant", false, null);

        // Assert
        Assert.Null(context.UserId);
    }

    [Fact]
    public void HasRole_WithExistingRole_ReturnsTrue()
    {
        // Arrange
        var roles = new[] { "admin", "user", "moderator" };
        var context = new TenantContext(Guid.NewGuid(), "tenant", true, "user123", roles);

        // Act & Assert
        Assert.True(context.HasRole("admin"));
        Assert.True(context.HasRole("user"));
        Assert.True(context.HasRole("moderator"));
    }

    [Fact]
    public void HasRole_WithNonExistingRole_ReturnsFalse()
    {
        // Arrange
        var roles = new[] { "admin", "user" };
        var context = new TenantContext(Guid.NewGuid(), "tenant", true, "user123", roles);

        // Act & Assert
        Assert.False(context.HasRole("superadmin"));
        Assert.False(context.HasRole("guest"));
    }

    [Fact]
    public void HasRole_WithDifferentCase_ReturnsTrueCaseInsensitive()
    {
        // Arrange
        var roles = new[] { "Admin", "User" };
        var context = new TenantContext(Guid.NewGuid(), "tenant", true, "user123", roles);

        // Act & Assert
        Assert.True(context.HasRole("admin"));
        Assert.True(context.HasRole("ADMIN"));
        Assert.True(context.HasRole("user"));
        Assert.True(context.HasRole("USER"));
    }

    [Fact]
    public void HasRole_WithNullRole_ReturnsFalse()
    {
        // Arrange
        var roles = new[] { "admin" };
        var context = new TenantContext(Guid.NewGuid(), "tenant", true, "user123", roles);

        // Act & Assert
        Assert.False(context.HasRole(null!));
    }

    [Fact]
    public void HasRole_WithEmptyRole_ReturnsFalse()
    {
        // Arrange
        var roles = new[] { "admin" };
        var context = new TenantContext(Guid.NewGuid(), "tenant", true, "user123", roles);

        // Act & Assert
        Assert.False(context.HasRole(string.Empty));
        Assert.False(context.HasRole(" "));
    }

    [Fact]
    public void HasRole_WithWhitespaceRole_ReturnsFalse()
    {
        // Arrange
        var roles = new[] { "admin" };
        var context = new TenantContext(Guid.NewGuid(), "tenant", true, "user123", roles);

        // Act & Assert
        Assert.False(context.HasRole("   "));
        Assert.False(context.HasRole("\t"));
        Assert.False(context.HasRole("\n"));
    }

    [Fact]
    public void HasRole_WithNoRoles_ReturnsFalse()
    {
        // Arrange
        var context = new TenantContext(Guid.NewGuid(), "tenant", true, "user123", null);

        // Act & Assert
        Assert.False(context.HasRole("admin"));
    }

    [Fact]
    public void Anonymous_CreatesAnonymousContext()
    {
        // Act
        var context = TenantContext.Anonymous();

        // Assert
        Assert.Equal(Guid.Empty, context.TenantId);
        Assert.Equal(string.Empty, context.TenantSlug);
        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Empty(context.Roles);
    }

    [Fact]
    public void System_CreatesSystemContext()
    {
        // Act
        var context = TenantContext.System();

        // Assert
        Assert.Equal(Guid.Empty, context.TenantId);
        Assert.Equal("system", context.TenantSlug);
        Assert.True(context.IsAuthenticated);
        Assert.Equal("system", context.UserId);
        Assert.Equal(2, context.Roles.Count);
        Assert.Contains("system", context.Roles);
        Assert.Contains("admin", context.Roles);
    }

    [Fact]
    public void System_HasSystemAndAdminRoles()
    {
        // Act
        var context = TenantContext.System();

        // Assert
        Assert.True(context.HasRole("system"));
        Assert.True(context.HasRole("admin"));
        Assert.True(context.HasRole("SYSTEM"));
        Assert.True(context.HasRole("ADMIN"));
        Assert.False(context.HasRole("user"));
    }

    [Fact]
    public void Roles_IsReadOnlyList()
    {
        // Arrange
        var roles = new List<string> { "admin", "user" };
        var context = new TenantContext(Guid.NewGuid(), "tenant", true, "user123", roles);

        // Act
        roles.Add("hacker"); // Try to modify the original list

        // Assert - The context's roles should not be affected
        Assert.Equal(2, context.Roles.Count);
        Assert.DoesNotContain("hacker", context.Roles);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_AllowsEmptyTenantId()
    {
        // Act
        var context = new TenantContext(Guid.Empty, "tenant", true, "user123");

        // Assert
        Assert.Equal(Guid.Empty, context.TenantId);
    }

    [Theory]
    [InlineData("admin", "admin", true)]
    [InlineData("Admin", "admin", true)]
    [InlineData("ADMIN", "admin", true)]
    [InlineData("admin", "Admin", true)]
    [InlineData("admin", "ADMIN", true)]
    [InlineData("user", "admin", false)]
    [InlineData("", "admin", false)]
    [InlineData(null, "admin", false)]
    public void HasRole_VariousCases_ReturnsExpectedResult(string? roleToCheck, string existingRole, bool expected)
    {
        // Arrange
        var context = new TenantContext(Guid.NewGuid(), "tenant", true, "user123", new[] { existingRole });

        // Act
        var result = context.HasRole(roleToCheck!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Multiple_FactoryMethods_CreateDifferentInstances()
    {
        // Act
        var anonymous1 = TenantContext.Anonymous();
        var anonymous2 = TenantContext.Anonymous();
        var system1 = TenantContext.System();
        var system2 = TenantContext.System();

        // Assert - Different instances
        Assert.NotSame(anonymous1, anonymous2);
        Assert.NotSame(system1, system2);
        Assert.NotSame(anonymous1, system1);

        // But with same values
        Assert.Equal(anonymous1.TenantId, anonymous2.TenantId);
        Assert.Equal(system1.TenantSlug, system2.TenantSlug);
    }
}
