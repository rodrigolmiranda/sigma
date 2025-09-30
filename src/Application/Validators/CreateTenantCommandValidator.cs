using FluentValidation;
using Sigma.Application.Commands;

namespace Sigma.Application.Validators;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required")
            .MaximumLength(100).WithMessage("Tenant name must not exceed 100 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Tenant slug is required")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens")
            .MaximumLength(50).WithMessage("Tenant slug must not exceed 50 characters");

        RuleFor(x => x.PlanType)
            .Must(BeValidPlanType).When(x => !string.IsNullOrEmpty(x.PlanType))
            .WithMessage("Plan type must be one of: free, starter, professional, enterprise");

        RuleFor(x => x.RetentionDays)
            .InclusiveBetween(1, 3650).WithMessage("Retention days must be between 1 and 3650 (10 years)");
    }

    private bool BeValidPlanType(string? planType)
    {
        var validTypes = new[] { "free", "starter", "professional", "enterprise" };
        return string.IsNullOrEmpty(planType) || validTypes.Contains(planType.ToLowerInvariant());
    }
}