using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using Sigma.Application.Contracts;
using Sigma.Application.Queries;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Sigma.Infrastructure.Persistence;

namespace Sigma.API.GraphQL;

public class Query
{
    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<Tenant>> GetTenants(
        [Service] ITenantRepository repository,
        CancellationToken cancellationToken)
    {
        var tenants = await repository.GetAllActiveAsync(cancellationToken);
        return tenants.AsQueryable();
    }

    [Authorize]
    public async Task<Tenant?> GetTenant(
        Guid id,
        [Service] IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var query = new GetTenantByIdQuery(id);
        var result = await dispatcher.DispatchAsync<GetTenantByIdQuery, Tenant>(query, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<Workspace>> GetWorkspaces(
        Guid tenantId,
        [Service] IWorkspaceRepository repository,
        CancellationToken cancellationToken)
    {
        var workspaces = await repository.GetByTenantIdAsync(tenantId, cancellationToken);
        return workspaces.AsQueryable();
    }

    [Authorize]
    public async Task<Workspace?> GetWorkspace(
        Guid id,
        Guid tenantId,
        [Service] IWorkspaceRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, tenantId, cancellationToken);
    }

    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<Channel>> GetChannels(
        Guid workspaceId,
        Guid tenantId,
        [Service] IChannelRepository repository,
        CancellationToken cancellationToken)
    {
        var channels = await repository.GetByWorkspaceIdAsync(workspaceId, tenantId, cancellationToken);
        return channels.AsQueryable();
    }

    [Authorize]
    public async Task<Channel?> GetChannel(
        Guid id,
        Guid tenantId,
        [Service] IChannelRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, tenantId, cancellationToken);
    }

    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<Message>> GetMessages(
        Guid channelId,
        Guid tenantId,
        [Service] IMessageRepository repository,
        CancellationToken cancellationToken,
        int limit = 100)
    {
        var messages = await repository.GetByChannelIdAsync(channelId, tenantId, limit, cancellationToken);
        return messages.AsQueryable();
    }

    [Authorize]
    public async Task<Message?> GetMessage(
        Guid id,
        Guid tenantId,
        [Service] IMessageRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, tenantId, cancellationToken);
    }

    public string GetVersion() => "1.0.0";

    public async Task<bool> GetHealthStatus(
        [Service] SigmaDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.CanConnectAsync(cancellationToken);
    }
}