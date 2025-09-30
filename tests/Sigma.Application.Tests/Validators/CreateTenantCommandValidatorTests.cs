using FluentValidation.TestHelper;
using Sigma.Application.Commands;
using Sigma.Application.Validators;
using Xunit;

namespace Sigma.Application.Tests.Validators;

public class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator;

    public CreateTenantCommandValidatorTests()
    {
        _validator = new CreateTenantCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "test-tenant", "free", 30);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand("", "test-tenant", "free", 30);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Tenant name is required");
    }

    [Fact]
    public void Validate_WithNameExceeding100Characters_ShouldHaveError()
    {
        // Arrange
        var longName = new string('a', 101);
        var command = new CreateTenantCommand(longName, "test-tenant", "free", 30);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Tenant name must not exceed 100 characters");
    }

    [Fact]
    public void Validate_WithEmptySlug_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "", "free", 30);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Tenant slug is required");
    }

    [Theory]
    [InlineData("Test Tenant")]
    [InlineData("test_tenant")]
    [InlineData("test.tenant")]
    [InlineData("TEST-TENANT")]
    public void Validate_WithInvalidSlugFormat_ShouldHaveError(string slug)
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", slug, "free", 30);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug must contain only lowercase letters, numbers, and hyphens");
    }

    [Theory]
    [InlineData("test-tenant")]
    [InlineData("test-tenant-123")]
    [InlineData("123-test")]
    public void Validate_WithValidSlugFormat_ShouldNotHaveError(string slug)
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", slug, "free", 30);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    [Theory]
    [InlineData("free")]
    [InlineData("starter")]
    [InlineData("professional")]
    [InlineData("enterprise")]
    [InlineData(null)]
    public void Validate_WithValidPlanType_ShouldNotHaveError(string? planType)
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "test-tenant", planType, 30);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PlanType);
    }

    [Fact]
    public void Validate_WithInvalidPlanType_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "test-tenant", "invalid", 30);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PlanType)
            .WithErrorMessage("Plan type must be one of: free, starter, professional, enterprise");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3651)]
    public void Validate_WithInvalidRetentionDays_ShouldHaveError(int retentionDays)
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "test-tenant", "free", retentionDays);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RetentionDays)
            .WithErrorMessage("Retention days must be between 1 and 3650 (10 years)");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    [InlineData(3650)]
    public void Validate_WithValidRetentionDays_ShouldNotHaveError(int retentionDays)
    {
        // Arrange
        var command = new CreateTenantCommand("Test Tenant", "test-tenant", "free", retentionDays);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RetentionDays);
    }
}