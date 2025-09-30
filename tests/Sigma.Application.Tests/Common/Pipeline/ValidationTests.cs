using Sigma.Application.Common.Pipeline;
using Xunit;

namespace Sigma.Application.Tests.Common.Pipeline;

public class ValidationResultTests
{
    [Fact]
    public void DefaultConstructor_ShouldCreateValidResult()
    {
        // Act
        var result = new ValidationResult();

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ConstructorWithErrors_ShouldCreateInvalidResult()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Name", "Name is required"),
            new ValidationError("Email", "Email is invalid")
        };

        // Act
        var result = new ValidationResult(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("Name", result.Errors[0].PropertyName);
        Assert.Equal("Name is required", result.Errors[0].Message);
        Assert.Equal("Email", result.Errors[1].PropertyName);
        Assert.Equal("Email is invalid", result.Errors[1].Message);
    }

    [Fact]
    public void ConstructorWithEmptyErrors_ShouldCreateInvalidResult()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Act
        var result = new ValidationResult(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Success_ShouldReturnValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FailureWithErrors_ShouldReturnInvalidResult()
    {
        // Arrange
        var error1 = new ValidationError("Field1", "Error 1");
        var error2 = new ValidationError("Field2", "Error 2");
        var error3 = new ValidationError("Field3", "Error 3");

        // Act
        var result = ValidationResult.Failure(error1, error2, error3);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.PropertyName == "Field1" && e.Message == "Error 1");
        Assert.Contains(result.Errors, e => e.PropertyName == "Field2" && e.Message == "Error 2");
        Assert.Contains(result.Errors, e => e.PropertyName == "Field3" && e.Message == "Error 3");
    }

    [Fact]
    public void FailureWithSingleError_ShouldReturnInvalidResult()
    {
        // Arrange
        var error = new ValidationError("Username", "Username already exists");

        // Act
        var result = ValidationResult.Failure(error);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Equal("Username", result.Errors[0].PropertyName);
        Assert.Equal("Username already exists", result.Errors[0].Message);
    }

    [Fact]
    public void FailureWithPropertyAndMessage_ShouldReturnInvalidResult()
    {
        // Act
        var result = ValidationResult.Failure("Password", "Password must be at least 8 characters");

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Equal("Password", result.Errors[0].PropertyName);
        Assert.Equal("Password must be at least 8 characters", result.Errors[0].Message);
    }

    [Fact]
    public void Errors_ShouldBeReadOnly()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError("Field1", "Error 1")
        };
        var result = new ValidationResult(errors);

        // Act & Assert
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<ValidationError>>(result.Errors);

        // Verify that adding to the original list doesn't affect the result
        errors.Add(new ValidationError("Field2", "Error 2"));
        Assert.Single(result.Errors);
    }

    [Fact]
    public void MultipleFailureScenarios_ShouldHandleCorrectly()
    {
        // Test with null property name
        var result1 = ValidationResult.Failure(new ValidationError(string.Empty, "General error"));
        Assert.False(result1.IsValid);
        Assert.Equal(string.Empty, result1.Errors[0].PropertyName);
        Assert.Equal("General error", result1.Errors[0].Message);

        // Test with multiple errors for same property
        var result2 = ValidationResult.Failure(
            new ValidationError("Email", "Email is required"),
            new ValidationError("Email", "Email format is invalid")
        );
        Assert.False(result2.IsValid);
        Assert.Equal(2, result2.Errors.Count);
        Assert.All(result2.Errors, e => Assert.Equal("Email", e.PropertyName));

        // Test with complex property paths
        var result3 = ValidationResult.Failure("Address.PostalCode", "Invalid postal code format");
        Assert.False(result3.IsValid);
        Assert.Equal("Address.PostalCode", result3.Errors[0].PropertyName);
    }
}

public class ValidationErrorTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Act
        var error = new ValidationError("FirstName", "First name is required");

        // Assert
        Assert.Equal("FirstName", error.PropertyName);
        Assert.Equal("First name is required", error.Message);
    }

    [Fact]
    public void Properties_ShouldBeReadOnly()
    {
        // Arrange & Act
        var error = new ValidationError("Age", "Age must be greater than 0");

        // Assert - Properties should only have getters
        Assert.Equal("Age", error.PropertyName);
        Assert.Equal("Age must be greater than 0", error.Message);

        // Verify properties are get-only (would not compile if they had setters)
        var properties = error.GetType().GetProperties();
        Assert.All(properties, p => Assert.False(p.CanWrite));
    }

    [Theory]
    [InlineData("", "Message cannot be empty")]
    [InlineData("   ", "Message with spaces")]
    [InlineData(null, "Null message")]
    [InlineData("Very.Long.Property.Path.With.Multiple.Segments", "Complex path error")]
    public void Constructor_ShouldHandleVariousInputs(string propertyName, string message)
    {
        // Act
        var error = new ValidationError(propertyName ?? string.Empty, message);

        // Assert
        Assert.Equal(propertyName ?? string.Empty, error.PropertyName);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void MultipleErrors_ShouldBeDistinct()
    {
        // Arrange & Act
        var error1 = new ValidationError("Field", "Error 1");
        var error2 = new ValidationError("Field", "Error 2");
        var error3 = new ValidationError("OtherField", "Error 1");

        // Assert - Each error instance should be distinct
        Assert.NotSame(error1, error2);
        Assert.NotSame(error1, error3);
        Assert.NotSame(error2, error3);

        // Even with same values
        var error4 = new ValidationError("Field", "Error 1");
        var error5 = new ValidationError("Field", "Error 1");
        Assert.NotSame(error4, error5);
        Assert.Equal(error4.PropertyName, error5.PropertyName);
        Assert.Equal(error4.Message, error5.Message);
    }

    [Fact]
    public void ErrorMessages_ShouldSupportVariousFormats()
    {
        // Test different message formats
        var errors = new[]
        {
            new ValidationError("Email", "The email address 'test@' is not valid."),
            new ValidationError("Password", "Password must contain at least: 1 uppercase, 1 lowercase, 1 number, 1 special character"),
            new ValidationError("DateOfBirth", "Date of birth must be in the past"),
            new ValidationError("Quantity", "Quantity must be between 1 and 100"),
            new ValidationError("Url", "The URL 'htp://invalid' is not a valid HTTP or HTTPS URL"),
            new ValidationError("PhoneNumber", "Phone number must be in format: +XX-XXX-XXX-XXXX"),
            new ValidationError("Items[0].Price", "Price must be greater than 0"),
            new ValidationError("Metadata.Tags", "Maximum of 10 tags allowed")
        };

        // Assert all errors are created successfully
        Assert.All(errors, error =>
        {
            Assert.NotNull(error.PropertyName);
            Assert.NotNull(error.Message);
            Assert.NotEmpty(error.Message);
        });
    }
}

public class ValidatorInterfaceTests
{
    [Fact]
    public async Task IValidator_ShouldBeImplementable()
    {
        // Arrange
        var validator = new TestValidator();
        var instance = new TestModel { Name = "Test", Value = 42 };

        // Act
        var result = await validator.ValidateAsync(instance);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task IValidator_WithInvalidData_ShouldReturnErrors()
    {
        // Arrange
        var validator = new TestValidator();
        var instance = new TestModel { Name = "", Value = -1 };

        // Act
        var result = await validator.ValidateAsync(instance);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task IValidator_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        var validator = new SlowValidator();
        var instance = new TestModel { Name = "Test", Value = 42 };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await validator.ValidateAsync(instance, cts.Token));
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private class TestValidator : IValidator<TestModel>
    {
        public Task<ValidationResult> ValidateAsync(TestModel instance, CancellationToken cancellationToken = default)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(instance.Name))
                errors.Add(new ValidationError(nameof(instance.Name), "Name is required"));

            if (instance.Value < 0)
                errors.Add(new ValidationError(nameof(instance.Value), "Value must be non-negative"));

            return Task.FromResult(errors.Any()
                ? new ValidationResult(errors)
                : ValidationResult.Success());
        }
    }

    private class SlowValidator : IValidator<TestModel>
    {
        public async Task<ValidationResult> ValidateAsync(TestModel instance, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken);
            return ValidationResult.Success();
        }
    }
}