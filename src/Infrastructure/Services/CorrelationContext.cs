using Microsoft.AspNetCore.Http;
using Sigma.Domain.Contracts;

namespace Sigma.Infrastructure.Services;

public class CorrelationContext : ICorrelationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString()
        ?? _httpContextAccessor.HttpContext?.TraceIdentifier;

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public string? TenantId
    {
        get
        {
            // Try to get from route values first
            if (_httpContextAccessor.HttpContext?.Request.RouteValues.TryGetValue("tenantId", out var tenantId) == true)
            {
                return tenantId?.ToString();
            }

            // Try to get from claims
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.Claims
                .FirstOrDefault(c => c.Type == "tenant_id" || c.Type == "TenantId");

            return tenantClaim?.Value;
        }
    }
}