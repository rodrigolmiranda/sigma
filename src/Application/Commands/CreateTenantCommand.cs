using Microsoft.Extensions.Logging;
using Sigma.Application.Contracts;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;

namespace Sigma.Application.Commands;

public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    string? PlanType,
    int RetentionDays) : ICommand<Guid>;

public sealed class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, Guid>
{
    private readonly ITenantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ITenantRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(CreateTenantCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating tenant with slug: {Slug}", command.Slug);

        var existingTenant = await _repository.GetBySlugAsync(command.Slug, cancellationToken);
        if (existingTenant != null)
        {
            _logger.LogWarning("Tenant creation failed - slug already exists: {Slug}", command.Slug);
            return Error.Conflict($"Tenant with slug '{command.Slug}' already exists");
        }

        Tenant tenant;
        try
        {
            tenant = new Tenant(
                command.Name,
                command.Slug,
                command.PlanType ?? "free",
                command.RetentionDays);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Tenant creation failed - validation error: {Message}", ex.Message);
            return Error.Validation(ex.Message);
        }

        await _repository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant created successfully with ID: {TenantId}, Slug: {Slug}",
            tenant.Id, tenant.Slug);

        return tenant.Id;
    }
}