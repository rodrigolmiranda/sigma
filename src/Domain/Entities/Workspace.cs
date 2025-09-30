using Sigma.Domain.Common;

namespace Sigma.Domain.Entities;

public class Workspace : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Platform { get; private set; }
    public string? ExternalId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastSyncAtUtc { get; private set; }

    private readonly List<Channel> _channels = new();
    public IReadOnlyList<Channel> Channels => _channels.AsReadOnly();

    private Workspace() : base()
    {
        Name = string.Empty;
        Platform = string.Empty;
        IsActive = true;
    }

    public Workspace(Guid tenantId, string name, string platform) : base()
    {
        TenantId = tenantId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Platform = platform ?? throw new ArgumentNullException(nameof(platform));
        IsActive = true;
    }

    public void UpdateExternalId(string externalId)
    {
        ExternalId = externalId;
    }

    public void UpdateLastSync()
    {
        LastSyncAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public Channel AddChannel(string name, string externalId)
    {
        var channel = new Channel(Id, name, externalId);
        _channels.Add(channel);
        return channel;
    }
}