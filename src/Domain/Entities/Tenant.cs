using Sigma.Domain.Common;
using Sigma.Shared.Enums;

namespace Sigma.Domain.Entities;

public class Tenant : Entity
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string PlanType { get; private set; }
    public int RetentionDays { get; private set; }
    public bool IsActive { get; private set; }
    public string? ExternalId { get; private set; }

    private readonly List<Workspace> _workspaces = new();
    public IReadOnlyList<Workspace> Workspaces => _workspaces.AsReadOnly();

    private Tenant() : base()
    {
        Name = string.Empty;
        Slug = string.Empty;
        PlanType = "free";
        IsActive = true;
        RetentionDays = 30;
    }

    public Tenant(string name, string slug, string planType = "free", int retentionDays = 30) : base()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));

        var validPlans = new[] { "free", "starter", "professional", "enterprise" };
        var normalizedPlan = (planType ?? "free").ToLowerInvariant();
        if (!validPlans.Contains(normalizedPlan))
            throw new ArgumentException($"Plan type must be one of: {string.Join(", ", validPlans)}", nameof(planType));

        PlanType = normalizedPlan;
        RetentionDays = retentionDays > 0 ? retentionDays : 30;
        IsActive = true;
    }

    public void UpdatePlan(string planType, int retentionDays)
    {
        if (string.IsNullOrWhiteSpace(planType))
            throw new ArgumentException("Plan type cannot be empty", nameof(planType));

        var validPlans = new[] { "free", "starter", "professional", "enterprise" };
        if (!validPlans.Contains(planType.ToLowerInvariant()))
            throw new ArgumentException($"Plan type must be one of: {string.Join(", ", validPlans)}", nameof(planType));

        if (retentionDays <= 0)
            throw new ArgumentException("Retention days must be positive", nameof(retentionDays));

        PlanType = planType.ToLowerInvariant();
        RetentionDays = retentionDays;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public Workspace AddWorkspace(string name, Platform platform)
    {
        var workspace = new Workspace(Id, name, platform);
        _workspaces.Add(workspace);
        return workspace;
    }
}