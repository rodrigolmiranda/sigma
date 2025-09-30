using Sigma.Application.Contracts;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;

namespace Sigma.Application.Commands;

public sealed record CreateWorkspaceCommand(
    Guid TenantId,
    string Name,
    string Platform,
    string? ExternalId = null) : ICommand<Guid>;

public sealed class CreateWorkspaceCommandHandler : ICommandHandler<CreateWorkspaceCommand, Guid>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWorkspaceCommandHandler(
        ITenantRepository tenantRepository,
        IWorkspaceRepository workspaceRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _workspaceRepository = workspaceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(CreateWorkspaceCommand command, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant == null)
        {
            return Error.NotFound($"Tenant with ID '{command.TenantId}' not found");
        }

        // Check for duplicate external ID
        if (!string.IsNullOrEmpty(command.ExternalId))
        {
            var existingWorkspace = await _workspaceRepository.GetByExternalIdAsync(
                command.ExternalId,
                command.Platform,
                command.TenantId,
                cancellationToken);

            if (existingWorkspace != null)
            {
                return Error.Conflict($"Workspace with external ID '{command.ExternalId}' already exists");
            }
        }

        var workspace = tenant.AddWorkspace(command.Name, command.Platform);

        if (!string.IsNullOrEmpty(command.ExternalId))
        {
            workspace.UpdateExternalId(command.ExternalId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workspace.Id;
    }
}