using HotChocolate;
using HotChocolate.Authorization;
using Sigma.Application.Commands;
using Sigma.Application.Contracts;
using Sigma.Application.Services;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Sigma.Shared.Enums;

namespace Sigma.API.GraphQL;

/// <summary>
/// GraphQL mutation root type containing all available mutations
/// </summary>
public class Mutation
{
    /// <summary>
    /// Creates a new tenant in the system
    /// </summary>
    /// <param name="input">The tenant creation input data</param>
    /// <param name="dispatcher">Command dispatcher service</param>
    /// <param name="repository">Tenant repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant payload with success status</returns>
    [Authorize(Roles = new[] { "Admin", "System" })]
    public async Task<CreateTenantPayload> CreateTenant(
        CreateTenantInput input,
        [Service] ICommandDispatcher dispatcher,
        [Service] ITenantRepository repository,
        CancellationToken cancellationToken)
    {
        var command = new CreateTenantCommand(
            input.Name,
            input.Slug,
            input.PlanType ?? "free",
            input.RetentionDays ?? 30);

        var result = await dispatcher.DispatchAsync<CreateTenantCommand, Guid>(command, cancellationToken);

        if (result.IsFailure)
        {
            return new CreateTenantPayload
            {
                Success = false,
                Errors = new[] { new UserError(result.Error?.Message ?? "Failed to create tenant", result.Error?.Code ?? "ERROR") }
            };
        }

        var tenant = await repository.GetByIdAsync(result.Value, cancellationToken);
        return new CreateTenantPayload
        {
            Tenant = tenant,
            Success = true
        };
    }

    [Authorize]
    public async Task<UpdateTenantPlanPayload> UpdateTenantPlan(
        UpdateTenantPlanInput input,
        [Service] ITenantRepository repository,
        [Service] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(input.TenantId, cancellationToken);
        if (tenant == null)
        {
            return new UpdateTenantPlanPayload
            {
                Success = false,
                Errors = new[] { new UserError("Tenant not found", "NOT_FOUND") }
            };
        }

        try
        {
            tenant.UpdatePlan(input.PlanType, input.RetentionDays);
            await repository.UpdateAsync(tenant, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateTenantPlanPayload
            {
                Tenant = tenant,
                Success = true
            };
        }
        catch (ArgumentException ex)
        {
            return new UpdateTenantPlanPayload
            {
                Success = false,
                Errors = new[] { new UserError(ex.Message, "VALIDATION_ERROR") }
            };
        }
    }

    [Authorize]
    public async Task<CreateWorkspacePayload> CreateWorkspace(
        CreateWorkspaceInput input,
        [Service] ICommandDispatcher dispatcher,
        [Service] IWorkspaceRepository repository,
        CancellationToken cancellationToken)
    {
        var command = new CreateWorkspaceCommand(
            input.TenantId,
            input.Name,
            input.Platform,
            input.ExternalId);

        var result = await dispatcher.DispatchAsync<CreateWorkspaceCommand, Guid>(command, cancellationToken);

        if (result.IsFailure)
        {
            return new CreateWorkspacePayload
            {
                Success = false,
                Errors = new[] { new UserError(result.Error?.Message ?? "Failed to create workspace", result.Error?.Code ?? "ERROR") }
            };
        }

        var workspace = await repository.GetByIdAsync(result.Value, input.TenantId, cancellationToken);
        return new CreateWorkspacePayload
        {
            Workspace = workspace,
            Success = true
        };
    }

    [Authorize]
    public async Task<CreateChannelPayload> CreateChannel(
        CreateChannelInput input,
        [Service] IWorkspaceRepository workspaceRepository,
        [Service] IChannelRepository channelRepository,
        [Service] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdAsync(input.WorkspaceId, input.TenantId, cancellationToken);
        if (workspace == null)
        {
            return new CreateChannelPayload
            {
                Success = false,
                Errors = new[] { new UserError("Workspace not found", "NOT_FOUND") }
            };
        }

        // Check for duplicate external ID
        if (!string.IsNullOrEmpty(input.ExternalId))
        {
            var existingChannel = await channelRepository.GetByExternalIdAsync(
                input.ExternalId,
                input.WorkspaceId,
                input.TenantId,
                cancellationToken);

            if (existingChannel != null)
            {
                return new CreateChannelPayload
                {
                    Success = false,
                    Errors = new[] { new UserError($"Channel with external ID '{input.ExternalId}' already exists", "CONFLICT") }
                };
            }
        }

        var channel = workspace.AddChannel(input.Name, input.ExternalId);
        await channelRepository.AddAsync(channel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateChannelPayload
        {
            Channel = channel,
            Success = true
        };
    }

    [Authorize]
    public async Task<PostSummaryPayload> PostSummary(
        PostSummaryInput input,
        [Service] ISummaryPoster summaryPoster,
        CancellationToken cancellationToken)
    {
        var success = await summaryPoster.PostSummaryAsync(
            input.TenantId,
            input.ChannelId,
            input.Period,
            cancellationToken);

        return new PostSummaryPayload
        {
            Success = success,
            Errors = success ? null : new[] { new UserError("Failed to post summary", "POSTING_ERROR") }
        };
    }
}

// Input types
public record CreateTenantInput(string Name, string Slug, string? PlanType, int? RetentionDays);
public record UpdateTenantPlanInput(Guid TenantId, string PlanType, int RetentionDays);
public record CreateWorkspaceInput(Guid TenantId, string Name, Platform Platform, string? ExternalId);
public record CreateChannelInput(Guid TenantId, Guid WorkspaceId, string Name, string ExternalId);
public record PostSummaryInput(Guid TenantId, Guid ChannelId, SummaryPeriod Period);

// Payload types
public class CreateTenantPayload : Payload
{
    public Tenant? Tenant { get; set; }
}

public class UpdateTenantPlanPayload : Payload
{
    public Tenant? Tenant { get; set; }
}

public class CreateWorkspacePayload : Payload
{
    public Workspace? Workspace { get; set; }
}

public class CreateChannelPayload : Payload
{
    public Channel? Channel { get; set; }
}

public class PostSummaryPayload : Payload
{
}

// Base payload
public abstract class Payload
{
    public bool Success { get; set; }
    public IReadOnlyList<UserError>? Errors { get; set; }
}

// Error type
public record UserError(string Message, string Code);