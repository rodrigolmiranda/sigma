using HotChocolate;
using HotChocolate.Types;
using Sigma.API.DataLoaders;
using Sigma.Domain.Entities;

namespace Sigma.API.GraphQL;

public class TenantType : ObjectType<Tenant>
{
    protected override void Configure(IObjectTypeDescriptor<Tenant> descriptor)
    {
        descriptor.Field(t => t.DomainEvents).Ignore();

        // Use DataLoader for workspaces
        descriptor
            .Field("workspaces")
            .Type<NonNullType<ListType<NonNullType<WorkspaceType>>>>()
            .ResolveWith<Resolvers>(r => r.GetWorkspacesAsync(default!, default!, default!));
    }

    private class Resolvers
    {
        public async Task<IEnumerable<Workspace>> GetWorkspacesAsync(
            [Parent] Tenant tenant,
            WorkspaceByTenantIdDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            return await dataLoader.LoadAsync(tenant.Id, cancellationToken) ?? Array.Empty<Workspace>();
        }
    }
}

public class WorkspaceType : ObjectType<Workspace>
{
    protected override void Configure(IObjectTypeDescriptor<Workspace> descriptor)
    {
        descriptor.Field(w => w.DomainEvents).Ignore();

        // Use DataLoader for channels
        descriptor
            .Field("channels")
            .Type<NonNullType<ListType<NonNullType<ChannelType>>>>()
            .ResolveWith<Resolvers>(r => r.GetChannelsAsync(default!, default!, default!));
    }

    private class Resolvers
    {
        public async Task<IEnumerable<Channel>> GetChannelsAsync(
            [Parent] Workspace workspace,
            ChannelByWorkspaceIdDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            return await dataLoader.LoadAsync(workspace.Id, cancellationToken) ?? Array.Empty<Channel>();
        }
    }
}

public class ChannelType : ObjectType<Channel>
{
    protected override void Configure(IObjectTypeDescriptor<Channel> descriptor)
    {
        descriptor.Field(c => c.DomainEvents).Ignore();

        // Use DataLoader for messages
        descriptor
            .Field("messages")
            .Type<NonNullType<ListType<NonNullType<MessageType>>>>()
            .ResolveWith<Resolvers>(r => r.GetMessagesAsync(default!, default!, default!));
    }

    private class Resolvers
    {
        public async Task<IEnumerable<Message>> GetMessagesAsync(
            [Parent] Channel channel,
            MessageByChannelIdDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            return await dataLoader.LoadAsync(channel.Id, cancellationToken) ?? Array.Empty<Message>();
        }
    }
}

public class MessageType : ObjectType<Message>
{
    protected override void Configure(IObjectTypeDescriptor<Message> descriptor)
    {
        descriptor.Field(m => m.DomainEvents).Ignore();
    }
}